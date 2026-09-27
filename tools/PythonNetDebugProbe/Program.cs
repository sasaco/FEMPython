using Python.Runtime;

const string pythonDllVariable = "PYTHONNET_PYDLL";
string? pythonDll = Environment.GetEnvironmentVariable(pythonDllVariable);
if (string.IsNullOrWhiteSpace(pythonDll))
{
    string repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    string configPath = Path.Combine(repositoryRoot, "FrameWeb", ".venv", "pyvenv.cfg");
    string? homeLine = File.ReadLines(configPath)
        .FirstOrDefault(line => line.StartsWith("home = ", StringComparison.OrdinalIgnoreCase));
    string pythonHomeFromVenv = homeLine?[7..].Trim()
        ?? throw new InvalidOperationException($"The uv environment has no home entry: {configPath}");
    pythonDll = Path.Combine(pythonHomeFromVenv, "python312.dll");
}

if (!File.Exists(pythonDll))
{
    throw new FileNotFoundException("The CPython DLL was not found.", pythonDll);
}

Runtime.PythonDLL = pythonDll;
string pythonHome = Path.GetDirectoryName(pythonDll)!;
PythonEngine.PythonHome = pythonHome;
PythonEngine.PythonPath = string.Join(Path.PathSeparator,
    Path.Combine(pythonHome, "Lib"),
    Path.Combine(pythonHome, "DLLs"),
    AppContext.BaseDirectory);
PythonEngine.Initialize();

try
{
    Console.WriteLine($"PID {Environment.ProcessId}; CPython {PythonEngine.Version}");
    Console.WriteLine(args.Contains("--breakpoint-gate", StringComparer.Ordinal)
        ? "Waiting for automated Python (native) and Native attach."
        : "Attach Visual Studio with Python, Managed and Native debugging, then press Enter.");
    Console.ReadLine();

    if (args.Contains("--breakpoint-gate", StringComparer.Ordinal))
    {
        using (Py.GIL())
        using (PyObject module = Py.Import("probe_calculation"))
        {
            Console.WriteLine("PYTHON_MODULE_READY");
        }

        Console.ReadLine();
    }

    using (Py.GIL())
    {
        using PyObject module = Py.Import("probe_calculation");
        using PyObject calculate = module.GetAttr("calculate");
        using PyObject span = 7.ToPython();
        using PyObject load = 5.ToPython();
        using PyObject result = calculate.Invoke(new[] { span, load }); // Set a C# breakpoint here; press F11.
        int value = result.As<int>();
        Console.WriteLine($"Python result: {value}");
        if (value != 27)
        {
            throw new InvalidOperationException($"Expected 27, got {value}.");
        }
    }
}
finally
{
    PythonEngine.Shutdown();
}
