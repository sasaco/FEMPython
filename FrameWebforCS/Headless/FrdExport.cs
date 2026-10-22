using System.Text.Json;
using Convert_Manager.FrameG;

namespace FrameWebforCS.Headless;

/// <summary>Options of <c>export-frd</c>: a saved 2D FrameWeb JSON to a FrameG .frd, without a calculation.</summary>
internal sealed record ExportFrdOptions(string Input, string OutputDirectory, string? Title)
{
    internal const string Command = "export-frd";

    internal static ExportFrdOptions Parse(string[] args)
    {
        if (args.Length == 0 || args[0] != Command || args.Length % 2 != 1)
            throw new RunnerException("INVALID_ARGUMENT", $"Expected {Command} followed by named option/value pairs.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] allowed = ["--input", "--output-dir", "--title"];
        for (int index = 1; index < args.Length; index += 2)
            if (!allowed.Contains(args[index]) || !values.TryAdd(args[index], args[index + 1]))
                throw new RunnerException("INVALID_ARGUMENT", $"Unknown or repeated option: {args[index]}");
        string AbsolutePath(string key)
        {
            if (!values.TryGetValue(key, out string? path) || string.IsNullOrWhiteSpace(path) ||
                !Path.IsPathFullyQualified(path))
                throw new RunnerException("INVALID_ARGUMENT", $"{key} requires an absolute path.");
            return Path.GetFullPath(path);
        }
        return new(AbsolutePath("--input"), AbsolutePath("--output-dir"), values.GetValueOrDefault("--title"));
    }
}

internal static class FrdExport
{
    internal const string ArtifactName = "model.frd";
    private const int MaxReportedErrors = 10;

    /// <summary>
    /// Writes model.frd. Every exporter diagnostic becomes a message with its JSON path; when any
    /// is an error, nothing is published and the envelope fails with FRD_EXPORT_UNSUPPORTED.
    /// </summary>
    internal static RunnerEnvelope Run(ExportFrdOptions options, CancellationToken cancellationToken)
    {
        HeadlessCalculation.ValidateOutputDirectory(options.OutputDirectory);
        string savedJson = HeadlessCalculation.ReadInput(options.Input);
        using (JsonDocument saved = JsonDocument.Parse(savedJson))
            HeadlessCalculation.ValidateDocument(saved.RootElement);

        FrdExportResult result = FrdExporter.Export(savedJson, new FrdExportOptions
        {
            Title = options.Title ?? Path.GetFileNameWithoutExtension(options.Input),
        });
        var messages = result.Diagnostics.Select(item => new RunnerMessage(
            item.Severity == FrdDiagnosticSeverity.Error ? "error" : "warning", item.Code, item.Message,
            item.Path.Length == 0 ? null : item.Path)).ToArray();
        var summary = new
        {
            nodes = result.NodeNumbers.Count,
            members = result.MemberNumbers.Count,
            loadCases = result.LoadCaseNumbers.Count,
            warnings = result.Warnings.Count(),
            errors = result.Errors.Count(),
        };
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToArray();
            string listed = string.Join("; ", errors.Take(MaxReportedErrors).Select(item => item.ToString()));
            string more = errors.Length > MaxReportedErrors ? $" (and {errors.Length - MaxReportedErrors} more)" : "";
            return RunnerEnvelope.Create(false, summary, messages: messages, error: new RunnerError(
                "FRD_EXPORT_UNSUPPORTED", $"FrameG cannot represent the input ({errors.Length} errors): {listed}{more}", "input"));
        }
        cancellationToken.ThrowIfCancellationRequested();
        var artifacts = HeadlessCalculation.Publish(options.OutputDirectory,
            [(ArtifactName, "frd", "application/octet-stream", result.Frd)], cancellationToken);
        return RunnerEnvelope.Create(true, summary, artifacts, messages);
    }
}
