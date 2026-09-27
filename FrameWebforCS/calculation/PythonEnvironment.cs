namespace FrameWebforCS.calculation;

internal sealed record PythonEnvironment(string PythonDll, string PythonHome, string PythonPath)
{
    public static string FindRepositoryRoot(string fromDirectory)
    {
        for (DirectoryInfo? directory = new(fromDirectory); directory is not null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")) &&
                Directory.Exists(Path.Combine(directory.FullName, "FrameWebforCS")))
                return directory.FullName;
        }
        throw new PythonCalculationException(PythonCalculationStage.Environment,
            "FrameWeb repository root was not found from the application directory.");
    }

    public static PythonEnvironment Resolve(string repositoryRoot)
    {
        if (IntPtr.Size != 8)
            throw new PythonCalculationException(PythonCalculationStage.Environment,
                "The calculation runtime requires an x64 process.");

        string frameWeb = Path.Combine(Path.GetFullPath(repositoryRoot), "FrameWeb");
        string virtualEnvironment = Path.Combine(frameWeb, ".venv");
        string configFile = Path.Combine(virtualEnvironment, "pyvenv.cfg");
        if (!File.Exists(configFile))
            throw Missing($"uv Python environment is missing: {configFile}");

        Dictionary<string, string> config = File.ReadLines(configFile)
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim(),
                StringComparer.OrdinalIgnoreCase);
        if (!config.TryGetValue("home", out string? home) || !Directory.Exists(home))
            throw Missing($"uv Python home is missing or invalid in: {configFile}");
        if (!config.TryGetValue("version_info", out string? version) ||
            !Version.TryParse(version, out Version? parsedVersion) || parsedVersion.Major != 3)
            throw Missing($"uv Python version is missing or invalid in: {configFile}");

        string dll = Path.Combine(home, $"python{parsedVersion.Major}{parsedVersion.Minor}.dll");
        string lib = Path.Combine(home, "Lib");
        string dlls = Path.Combine(home, "DLLs");
        string sitePackages = Path.Combine(virtualEnvironment, "Lib", "site-packages");
        string source = Path.Combine(frameWeb, "src");
        foreach (string path in new[] { dll, lib, dlls, sitePackages, source })
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                throw Missing($"Required Python runtime path is missing: {path}");
        }
        return new PythonEnvironment(dll, home,
            string.Join(Path.PathSeparator, lib, dlls, sitePackages, source));
    }

    private static PythonCalculationException Missing(string message) =>
        new(PythonCalculationStage.Environment, message);
}
