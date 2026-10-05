namespace FrameWebforCS.Headless;

internal sealed class RunnerException(string code, string message, string category = "input")
    : Exception(message)
{
    internal string Code { get; } = code;
    internal string Category { get; } = category;
}

internal sealed record RunOptions(string Input, string OutputDirectory, bool GeneratePdf,
    bool GeneratePik, IReadOnlyList<string> PdfSections)
{
    internal static readonly string[] DefaultSections =
        ["input", "section_force", "pickup_section_force", "displacement", "pickup_displacement"];

    internal static RunOptions Parse(string[] args)
    {
        if (args.Length == 0 || args[0] != "run" || args.Length % 2 != 1)
            throw new RunnerException("INVALID_ARGUMENT", "Expected run followed by named option/value pairs.");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] allowed = ["--input", "--output-dir", "--generate-pdf", "--generate-pik", "--pdf-sections"];
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
        bool Flag(string key) => !values.TryGetValue(key, out string? value) ? true : value switch
        {
            "true" => true, "false" => false,
            _ => throw new RunnerException("INVALID_ARGUMENT", $"{key} must be true or false."),
        };
        string[] sections = values.TryGetValue("--pdf-sections", out string? sectionText)
            ? sectionText.Split(',') : [.. DefaultSections];
        if (sections.Length == 0 || sections.Any(section => !DefaultSections.Contains(section)) ||
            sections.Distinct(StringComparer.Ordinal).Count() != sections.Length)
            throw new RunnerException("INVALID_ARGUMENT", "PDF sections must be distinct supported section names.");
        return new(AbsolutePath("--input"), AbsolutePath("--output-dir"),
            Flag("--generate-pdf"), Flag("--generate-pik"), sections);
    }
}
