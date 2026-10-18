namespace FrameWebforCS.Headless;

internal sealed class RunnerException(string code, string message, string category = "input")
    : Exception(message)
{
    internal string Code { get; } = code;
    internal string Category { get; } = category;
}

internal sealed record RunOptions(string Input, string OutputDirectory, bool GeneratePdf,
    bool GeneratePik, IReadOnlyList<string> PdfSections,
    bool GeneratePickupDisplacementCsv = false, bool GeneratePickupReactionCsv = false,
    string PdfPaper = "A4", string PdfOrientation = "vertical",
    string LoadDiagramLayout = "single", bool LoadDiagramAxis = true)
{
    internal static readonly string[] DefaultSections =
        ["input", "section_force", "pickup_section_force", "displacement", "pickup_displacement"];
    internal const string LoadDiagramSection = "load_diagram";
    internal static readonly string[] SupportedSections = [.. DefaultSections, LoadDiagramSection];
    private static readonly string[] LoadDiagramOptions = ["--load-diagram-layout", "--load-diagram-axis"];

    /// <summary>
    /// A load diagram draws the saved loads only; when it is the whole request the runner
    /// neither starts Python nor calculates.
    /// </summary>
    internal bool LoadDiagramOnly => GeneratePdf && PdfSections.SequenceEqual([LoadDiagramSection]) &&
        !GeneratePik && !GeneratePickupDisplacementCsv && !GeneratePickupReactionCsv;

    internal static RunOptions Parse(string[] args)
    {
        if (args.Length == 0 || args[0] != "run" || args.Length % 2 != 1)
            throw new RunnerException("INVALID_ARGUMENT", "Expected run followed by named option/value pairs.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] allowed = ["--input", "--output-dir", "--generate-pdf", "--generate-pik", "--pdf-sections",
            "--generate-pickup-displacement-csv", "--generate-pickup-reaction-csv",
            "--pdf-paper", "--pdf-orientation", .. LoadDiagramOptions];
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
        bool Flag(string key, bool defaultValue = true) => !values.TryGetValue(key, out string? value) ? defaultValue : value switch
        {
            "true" => true, "false" => false,
            _ => throw new RunnerException("INVALID_ARGUMENT", $"{key} must be true or false."),
        };
        string Choice(string key, string defaultValue, params string[] choices)
        {
            if (!values.TryGetValue(key, out string? value)) return defaultValue;
            if (!choices.Contains(value))
                throw new RunnerException("INVALID_ARGUMENT", $"{key} must be one of {string.Join(", ", choices)}.");
            return value;
        }
        string[] sections = values.TryGetValue("--pdf-sections", out string? sectionText)
            ? sectionText.Split(',') : [.. DefaultSections];
        if (sections.Length == 0 || sections.Any(section => !SupportedSections.Contains(section)) ||
            sections.Distinct(StringComparer.Ordinal).Count() != sections.Length)
            throw new RunnerException("INVALID_ARGUMENT", "PDF sections must be distinct supported section names.");
        bool generatePdf = Flag("--generate-pdf");
        if (LoadDiagramOptions.Any(values.ContainsKey) && !(generatePdf && sections.Contains(LoadDiagramSection)))
            throw new RunnerException("INVALID_ARGUMENT", "Load diagram options require the load_diagram PDF section.");
        if ((values.ContainsKey("--pdf-paper") || values.ContainsKey("--pdf-orientation")) && !generatePdf)
            throw new RunnerException("INVALID_ARGUMENT", "PDF paper and orientation require PDF output.");
        return new(AbsolutePath("--input"), AbsolutePath("--output-dir"),
            generatePdf, Flag("--generate-pik"), sections,
            Flag("--generate-pickup-displacement-csv", false), Flag("--generate-pickup-reaction-csv", false),
            Choice("--pdf-paper", "A4", "A4", "A3"),
            Choice("--pdf-orientation", "vertical", "vertical", "horizontal"),
            Choice("--load-diagram-layout", "single", "single", "split_horizontal", "split_vertical"),
            Flag("--load-diagram-axis"));
    }
}
