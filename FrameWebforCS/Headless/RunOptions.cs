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
    string LoadDiagramLayout = "single", bool LoadDiagramAxis = true,
    string SectionDiagramLayout = "single", IReadOnlyList<string>? SectionDiagramComponents = null,
    IReadOnlyList<string>? LoadDiagramSingleCases = null)
{
    internal static readonly string[] DefaultSections =
        ["input", "section_force", "pickup_section_force", "displacement", "pickup_displacement"];
    internal const string LoadDiagramSection = "load_diagram";
    internal const string SectionDiagramSection = "section_diagram";
    internal const string PickupSectionDiagramSection = "pickup_section_diagram";
    internal static readonly string[] SectionDiagramSections = [SectionDiagramSection, PickupSectionDiagramSection];
    internal static readonly string[] SupportedSections = [.. DefaultSections, LoadDiagramSection, .. SectionDiagramSections];
    private static readonly string[] LoadDiagramOptions = ["--load-diagram-layout", "--load-diagram-axis",
        "--load-diagram-single-cases"];
    private static readonly string[] SectionDiagramOptions = ["--section-diagram-layout", "--section-diagram-components"];
    /// <summary>The components a section diagram may draw; a PICKUP diagram has no displacement.</summary>
    internal static readonly string[] SectionDiagramComponentNames = ["mz", "fy", "fx", "disg"];
    private static readonly string[] Layouts = ["single", "split_horizontal", "split_vertical"];

    internal bool HasSectionDiagram => GeneratePdf && PdfSections.Any(SectionDiagramSections.Contains);

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
            "--pdf-paper", "--pdf-orientation", .. LoadDiagramOptions, .. SectionDiagramOptions];
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
        if (SectionDiagramOptions.Any(values.ContainsKey) && !(generatePdf && sections.Any(SectionDiagramSections.Contains)))
            throw new RunnerException("INVALID_ARGUMENT",
                "Section diagram options require the section_diagram or pickup_section_diagram PDF section.");
        if ((values.ContainsKey("--pdf-paper") || values.ContainsKey("--pdf-orientation")) && !generatePdf)
            throw new RunnerException("INVALID_ARGUMENT", "PDF paper and orientation require PDF output.");
        string loadLayout = Choice("--load-diagram-layout", "single", Layouts);
        string sectionLayout = Choice("--section-diagram-layout", "single", Layouts);
        // The printed document has one diagram layout, shared by the load and section diagrams.
        if (sections.Contains(LoadDiagramSection) && sections.Any(SectionDiagramSections.Contains) &&
            loadLayout != sectionLayout)
            throw new RunnerException("INVALID_ARGUMENT",
                "The load diagram and section diagram layouts must be the same in one PDF.");
        string[]? singleCases = null;
        if (values.TryGetValue("--load-diagram-single-cases", out string? singleText))
        {
            // Load cases printed alone on a page of a split load diagram (FrameG's two-tier pages).
            singleCases = singleText.Split(',');
            if (singleCases.Any(string.IsNullOrWhiteSpace) ||
                singleCases.Distinct(StringComparer.Ordinal).Count() != singleCases.Length)
                throw new RunnerException("INVALID_ARGUMENT",
                    "--load-diagram-single-cases must be distinct load case keys separated by commas.");
            if (loadLayout == "single")
                throw new RunnerException("INVALID_ARGUMENT",
                    "--load-diagram-single-cases requires --load-diagram-layout split_horizontal or split_vertical.");
        }
        string[]? components = null;
        if (values.TryGetValue("--section-diagram-components", out string? componentText))
        {
            components = componentText.Split(',');
            if (components.Length == 0 || components.Any(name => !SectionDiagramComponentNames.Contains(name)) ||
                components.Distinct(StringComparer.Ordinal).Count() != components.Length)
                throw new RunnerException("INVALID_ARGUMENT",
                    $"--section-diagram-components must be distinct names of {string.Join(", ", SectionDiagramComponentNames)}.");
            if (sections.Contains(PickupSectionDiagramSection) && components.All(name => name == "disg"))
                throw new RunnerException("INVALID_ARGUMENT",
                    "The pickup_section_diagram section draws section forces only; select mz, fy or fx.");
        }
        return new(AbsolutePath("--input"), AbsolutePath("--output-dir"),
            generatePdf, Flag("--generate-pik"), sections,
            Flag("--generate-pickup-displacement-csv", false), Flag("--generate-pickup-reaction-csv", false),
            Choice("--pdf-paper", "A4", "A4", "A3"),
            Choice("--pdf-orientation", "vertical", "vertical", "horizontal"),
            loadLayout, Flag("--load-diagram-axis"), sectionLayout, components, singleCases);
    }
}
