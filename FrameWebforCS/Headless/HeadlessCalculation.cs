using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FrameWebforCS.calculation;
using FrameWebforCS.components.input;
using FrameWebforCS.providers.printing;
using PDF_Manager;

namespace FrameWebforCS.Headless;

internal static class HeadlessCalculation
{
    internal const int MaxInputBytes = 32 * 1024 * 1024;
    internal const int MaxArtifactBytes = 128 * 1024 * 1024;
    private static readonly UTF8Encoding Utf8 = new(false, true);

    internal static async Task<RunnerEnvelope> RunAsync(RunOptions options, CancellationToken cancellationToken)
    {
        ValidateOutputDirectory(options.OutputDirectory);
        string savedJson = ReadInput(options.Input);
        using JsonDocument saved = JsonDocument.Parse(savedJson);
        ValidateDocument(saved.RootElement);
        int dimension = saved.RootElement.TryGetProperty("dimension", out JsonElement dimensionValue)
            ? dimensionValue.GetInt32() : 3;
        if (options.GeneratePik && dimension != 2)
            throw new RunnerException("PIK_REQUIRES_2D", "PIK output requires a 2D model; disable PIK for a 3D calculation.");
        var rows = InputCombineService.ParseCombineJson(saved.RootElement);
        bool needsPickup = options.GeneratePik || options.GeneratePdf && options.PdfSections.Any(section => section.StartsWith("pickup_", StringComparison.Ordinal));
        if (needsPickup && rows.Pickup.Count == 0)
            throw new RunnerException("PICKUP_REQUIRED", "Requested output requires at least one PICKUP definition.");
        CalculationRequest request = await Task.Run(() => CalculationRequestBuilder.FromSavedJson(savedJson), cancellationToken)
            .WaitAsync(cancellationToken);
        var snapshot = CalculationDerivedInputSnapshot.Capture(rows.Define, rows.Combine, rows.Pickup);
        // Empty saved load cases are intentionally omitted by the desktop request projection.
        // They remain valid references and contribute nothing in the shared derived presenter.
        ValidateDerivedReferences(snapshot, saved.RootElement.GetProperty("load")
            .EnumerateObject().Select(item => item.Name).ToHashSet(StringComparer.Ordinal));
        using JsonDocument projected = JsonDocument.Parse(request.Json);
        var analyzedLoadIds = projected.RootElement.GetProperty("load").EnumerateObject()
            .Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        string[] skippedLoadIds = saved.RootElement.GetProperty("load").EnumerateObject()
            .Select(item => item.Name).Where(id => !analyzedLoadIds.Contains(id)).ToArray();

        string canonicalJson;
        var runtime = new PythonCalculationRuntime(redirectOutputToStderr: true);
        try { canonicalJson = await runtime.CalculateAsync(request.Json, cancellationToken); }
        finally
        {
            // A native solve cannot be interrupted in process. On cancellation the process exits;
            // the MCP caller also owns a hard process deadline. Do not wait on a native solve.
            if (!cancellationToken.IsCancellationRequested)
                await runtime.DisposeAsync().AsTask().WaitAsync(cancellationToken);
        }
        if (Utf8.GetByteCount(canonicalJson) > MaxArtifactBytes)
            throw new RunnerException("OUTPUT_TOO_LARGE", "Calculation result exceeds 128 MiB.", "output");
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(canonicalJson);
        CalculationDerivedPresentation derived = await Task.Run(() =>
            CalculationDerivedPresenter.Build(result, dimension, snapshot), cancellationToken).WaitAsync(cancellationToken);
        var presentation = new CalculationResultPresentation(result, derived, dimension);
        if (needsPickup && (derived.Pickups.Count != rows.Pickup.Count || derived.Pickups.Any(pickup =>
            !pickup.SectionForces.Values.Any(values => values.Count > 0) ||
            !pickup.Displacements.Values.Any(values => values.Count > 0))))
            throw new RunnerException("PICKUP_UNAVAILABLE", "One or more PICKUP definitions have no calculated results.", "calculation");

        using JsonDocument canonical = JsonDocument.Parse(canonicalJson);
        object[] DerivedCases(IReadOnlyList<CalculationDerivedCase> cases) => cases.Select(item => (object)new
        {
            item.Id, item.Name, item.Displacements, item.Reactions, item.SectionForces,
        }).ToArray();
        byte[] resultBytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            schemaVersion = 1, kind = "frameweb_analysis", dimension,
            skippedLoadCases = skippedLoadIds,
            analysisResultSet = canonical.RootElement,
            derived = new { defines = DerivedCases(derived.Defines), combines = DerivedCases(derived.Combines), pickups = DerivedCases(derived.Pickups) },
        }, Program.JsonOptions);
        var outputs = new List<(string Name, string Kind, string MediaType, byte[] Bytes)>
        {
            ("result.json", "result", "application/json", resultBytes),
        };
        if (options.GeneratePik)
        {
            string pik = PickupExportFormatter.Format(presentation);
            if (pik.Count(character => character == '\n') <= 1)
                throw new RunnerException("EMPTY_PIK", "PIK output contains no data rows.", "output");
            outputs.Add(("pickup.pik", "pik", "text/plain; charset=utf-8", Utf8.GetBytes(pik)));
        }
        if (options.GeneratePdf)
        {
            var selection = new PrintSelection(options.PdfSections.Select(section => section switch
            {
                "input" => PrintOption.Input,
                "section_force" => PrintOption.SectionForce,
                "pickup_section_force" => PrintOption.PickupSectionForce,
                "displacement" => PrintOption.Displacement,
                "pickup_displacement" => PrintOption.PickupDisplacement,
                _ => throw new RunnerException("INVALID_ARGUMENT", "Unsupported PDF section."),
            }).ToArray());
            var printSnapshot = new PrintSnapshot(selection, savedJson, 0, 0, presentation, derived, []);
            byte[] pdf = await Task.Run(() => DirectPdfGenerator.Generate(
                PrintProjection.Build(printSnapshot), cancellationToken), cancellationToken).WaitAsync(cancellationToken);
            if (pdf.Length < 8 || !pdf.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
                throw new RunnerException("INVALID_PDF", "PDF generation returned an invalid document.", "output");
            outputs.Add(("report.pdf", "pdf", "application/pdf", pdf));
        }

        var messages = skippedLoadIds.Take(50).Select(id => new RunnerMessage("warning",
            "EMPTY_LOAD_CASE_SKIPPED", $"Saved load case {id} has no effective loads and was omitted by the shared desktop request projection.",
            "load." + id)).ToList();
        if (skippedLoadIds.Length > 50)
            messages.Add(new("warning", "EMPTY_LOAD_CASE_SKIPPED",
                $"{skippedLoadIds.Length - 50} further empty load cases were omitted; see skippedLoadCases in result.json."));
        foreach (AnalysisResult item in result.Results)
        {
            IReadOnlyList<string> warnings = item switch
            {
                StaticAnalysisResult value => value.Diagnostics.Warnings,
                LoadStepAnalysisResult value => value.Diagnostics.Warnings,
                ModalAnalysisResult value => value.Diagnostics.Warnings,
                _ => [],
            };
            messages.AddRange(warnings.Select(warning => new RunnerMessage("warning", "SOLVER_WARNING", warning, item.CaseId)));
        }
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RunnerArtifact> artifacts = Publish(options.OutputDirectory, outputs, cancellationToken);
        return RunnerEnvelope.Create(true, new
        {
            dimension, nodes = result.Topology.Nodes.Count, members = result.Topology.Members.Count,
            cases = result.Cases.Count, results = result.Results.Count, pickups = derived.Pickups.Count,
            skippedLoadCases = skippedLoadIds.Length,
            pdfSections = options.GeneratePdf ? options.PdfSections : [],
        }, artifacts, messages);
    }

    internal static string ReadInput(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length is < 2 or > MaxInputBytes)
            throw new RunnerException("INVALID_INPUT_SIZE", "Input must contain 2 bytes to 32 MiB of UTF-8 JSON.");
        using var reader = new StreamReader(stream, Utf8, detectEncodingFromByteOrderMarks: false);
        string text = reader.ReadToEnd();
        return text.Length > 0 && text[0] == '\uFEFF' ? text[1..] : text;
    }

    private static void ValidateDerivedReferences(CalculationDerivedInputSnapshot snapshot,
        HashSet<string> caseIds)
    {
        // The desktop presenter tolerates incomplete edit rows. A noninteractive export must
        // not silently omit a requested term and publish a partial combination or envelope.
        foreach (var definition in snapshot.Definitions)
            foreach (int signedCase in definition.SignedCases)
                if (signedCase != 0 && (signedCase == int.MinValue ||
                    !caseIds.Contains(Math.Abs(signedCase).ToString(System.Globalization.CultureInfo.InvariantCulture))))
                    throw new RunnerException("INVALID_DERIVED_REFERENCE", $"DEFINE {definition.Id} references an unavailable load case.");
        HashSet<string> definitionIds = snapshot.Definitions.Count == 0 ? caseIds :
            snapshot.Definitions.Where(item => item.SignedCases.Count > 0)
                .Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var combination in snapshot.Combinations)
            foreach (var term in combination.Terms.Where(item => item.Coefficient != 0))
                if (!definitionIds.Contains(term.Id))
                    throw new RunnerException("INVALID_DERIVED_REFERENCE", $"COMBINE {combination.Id} references an unavailable DEFINE.");
        HashSet<string> combinationIds = snapshot.Combinations
            .Where(item => item.Terms.Any(term => term.Coefficient != 0))
            .Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var pickup in snapshot.Pickups)
            foreach (string id in pickup.CombinationIds)
                if (!combinationIds.Contains(id))
                    throw new RunnerException("INVALID_DERIVED_REFERENCE", $"PICKUP {pickup.Id} references an unavailable COMBINE.");
    }

    internal static void ValidateDocument(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object)
            throw new RunnerException("INVALID_INPUT", "Input JSON must be an object.");
        void Check(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (!names.Add(property.Name))
                        throw new RunnerException("INVALID_INPUT", "Input JSON contains a duplicate property.");
                    Check(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
                foreach (JsonElement item in element.EnumerateArray()) Check(item);
        }
        Check(root);
        if (root.TryGetProperty("dimension", out JsonElement dimension) &&
            (dimension.ValueKind != JsonValueKind.Number || !dimension.TryGetInt32(out int value) || value is not (2 or 3)))
            throw new RunnerException("INVALID_INPUT", "dimension must be 2 or 3.");
    }

    internal static void ValidateOutputDirectory(string outputDirectory)
    {
        if (File.Exists(outputDirectory) || Directory.Exists(outputDirectory) && Directory.EnumerateFileSystemEntries(outputDirectory).Any())
            throw new RunnerException("OUTPUT_NOT_EMPTY", "Output directory must be absent or empty.", "output");
        for (DirectoryInfo? directory = new(outputDirectory); directory is not null; directory = directory.Parent)
            if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new RunnerException("UNSAFE_OUTPUT_PATH", "Output directory must not traverse a link or junction.", "output");
    }

    private static IReadOnlyList<RunnerArtifact> Publish(string outputDirectory,
        IReadOnlyList<(string Name, string Kind, string MediaType, byte[] Bytes)> outputs, CancellationToken token)
    {
        foreach (var output in outputs)
            if (output.Bytes.Length is < 1 or > MaxArtifactBytes)
                throw new RunnerException("OUTPUT_TOO_LARGE", "Each output must contain 1 byte to 128 MiB.", "output");
        ValidateOutputDirectory(outputDirectory);
        Directory.CreateDirectory(outputDirectory);
        string stage = Path.Combine(outputDirectory, ".staging-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        var published = new List<string>();
        try
        {
            foreach (var output in outputs)
            {
                token.ThrowIfCancellationRequested();
                File.WriteAllBytes(Path.Combine(stage, output.Name), output.Bytes);
            }
            var artifacts = outputs.Select(output => new RunnerArtifact(output.Kind, output.MediaType,
                output.Name, output.Bytes.LongLength, Convert.ToHexStringLower(SHA256.HashData(output.Bytes)))).ToArray();
            foreach (var output in outputs)
            {
                token.ThrowIfCancellationRequested();
                string target = Path.Combine(outputDirectory, output.Name);
                File.Move(Path.Combine(stage, output.Name), target, overwrite: false);
                published.Add(target);
            }
            Directory.Delete(stage);
            return artifacts;
        }
        catch
        {
            foreach (string file in published) File.Delete(file);
            throw;
        }
        finally
        {
            if (Directory.Exists(stage)) Directory.Delete(stage, recursive: true);
        }
    }
}
