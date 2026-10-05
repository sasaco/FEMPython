using System.Collections.Concurrent;
using System.Text.Json;
using Python.Runtime;

namespace FrameWebforCS.calculation;

public enum PythonCalculationStage
{
    Environment,
    Initialization,
    Import,
    Solver,
    Conversion,
    Debugger
}

public sealed class PythonCalculationException : Exception
{
    public PythonCalculationStage Stage { get; }

    public PythonCalculationException(PythonCalculationStage stage, string message, Exception? inner = null)
        : base(message, inner) => Stage = stage;
}

/// <summary>Owns the one CPython interpreter and its initialization thread.</summary>
public sealed class PythonCalculationRuntime : IAsyncDisposable
{
    private static int _pythonLifetimeStarted;
    private sealed record Work(string Json, CancellationToken CancellationToken,
        TaskCompletionSource<string> Completion, TaskCompletionSource Idle, bool Stop = false);

    private readonly BlockingCollection<Work> _queue = new();
    private readonly Thread _thread;
    private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly string _repositoryRoot;
    private readonly string _moduleName;
    private readonly bool _redirectOutputToStderr;
    private readonly ICalculationDebugGate? _debugGate;
    private readonly object _admission = new();
    private TaskCompletionSource _idle = CompletedIdle();
    private int _busy;
    private int _closing;

    public PythonCalculationRuntime(string? repositoryRoot = null, ICalculationDebugGate? debugGate = null,
        bool redirectOutputToStderr = false)
        : this(repositoryRoot, debugGate, "fem.analysis_result_sets", redirectOutputToStderr) { }

    internal PythonCalculationRuntime(string? repositoryRoot, ICalculationDebugGate? debugGate, string moduleName,
        bool redirectOutputToStderr = false)
    {
        _repositoryRoot = repositoryRoot ?? PythonEnvironment.FindRepositoryRoot(AppContext.BaseDirectory);
        _moduleName = moduleName;
        _redirectOutputToStderr = redirectOutputToStderr;
#if DEBUG
        _debugGate = debugGate ?? (Environment.GetEnvironmentVariable("FRAMEWEB_PYTHON_DEBUG_GATE") == "1"
            ? new CalculationDebugPipeGate() : null);
#else
        _debugGate = null;
#endif
        _thread = new Thread(Run) { IsBackground = true, Name = "FrameWeb CPython runtime" };
        _thread.Start();
    }

    /// <summary>Runs a calculation off the UI thread. Cancellation discards its eventual result.</summary>
    public Task<string> CalculateAsync(string requestJson, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestJson);
        cancellationToken.ThrowIfCancellationRequested();
        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_admission)
        {
            if (_closing != 0)
                throw new ObjectDisposedException(nameof(PythonCalculationRuntime));
            if (_busy != 0)
                throw new InvalidOperationException("A Python calculation is already active.");
            _busy = 1;
            _idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _queue.Add(new Work(requestJson, cancellationToken, completion, _idle));
        }
        return completion.Task.WaitAsync(cancellationToken);
    }

    /// <summary>Completes after the active native solve and its cleanup finish.</summary>
    public Task WaitForIdleAsync()
    {
        lock (_admission) return _idle.Task;
    }

    private static TaskCompletionSource CompletedIdle()
    {
        var idle = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        idle.SetResult();
        return idle;
    }

    public async ValueTask DisposeAsync()
    {
        lock (_admission)
        {
            if (_closing == 0)
            {
                _closing = 1;
                _queue.Add(new Work("", CancellationToken.None,
                    new TaskCompletionSource<string>(), new TaskCompletionSource(), Stop: true));
                _queue.CompleteAdding();
            }
        }
        try { await _stopped.Task.ConfigureAwait(false); }
        finally
        {
            if (_debugGate is not null)
                await _debugGate.DisposeAsync().ConfigureAwait(false);
            _queue.Dispose();
        }
    }

    private void Run()
    {
        bool initialized = false;
        IntPtr threadState = IntPtr.Zero;
        PyObject? module = null;
        PyObject? jsonModule = null;
        bool attached = false;
        bool ready = false;
        PythonCalculationException? startupFailure = null;
        try
        {
            foreach (Work work in _queue.GetConsumingEnumerable())
            {
                if (work.Stop) break;
                string? completedJson = null;
                Exception? failure = null;
                bool canceled = false;
                try
                {
                    work.CancellationToken.ThrowIfCancellationRequested();
                    if (startupFailure is not null) throw startupFailure;
                    if (!initialized)
                    {
                        try
                        {
                            PythonEnvironment environment = PythonEnvironment.Resolve(_repositoryRoot);
                            if (Interlocked.CompareExchange(ref _pythonLifetimeStarted, 1, 0) != 0)
                                throw new PythonCalculationException(PythonCalculationStage.Initialization,
                                    "CPython can be initialized only once in this process. Reuse the runtime owner.");
                            Runtime.PythonDLL = environment.PythonDll;
                            PythonEngine.PythonHome = environment.PythonHome;
                            PythonEngine.PythonPath = environment.PythonPath;
                            PythonEngine.Initialize();
                            initialized = true;
                            if (_redirectOutputToStderr)
                            {
                                using PyObject sys = Py.Import("sys");
                                using PyObject stderr = sys.GetAttr("stderr");
                                sys.SetAttr("stdout", stderr);
                            }
                            threadState = PythonEngine.BeginAllowThreads();
                        }
                        catch (PythonCalculationException) { throw; }
                        catch (Exception error)
                        {
                            throw new PythonCalculationException(PythonCalculationStage.Initialization,
                                "CPython could not be initialized.", error);
                        }
                    }
                    if (!ready)
                    {
                        if (!attached)
                        {
                            try
                            {
                                _debugGate?.WaitForAttachAsync(Environment.ProcessId, work.CancellationToken)
                                    .GetAwaiter().GetResult();
                                attached = true;
                            }
                            catch (OperationCanceledException) when (work.CancellationToken.IsCancellationRequested) { throw; }
                            catch (Exception error)
                            {
                                throw new PythonCalculationException(PythonCalculationStage.Debugger,
                                    "The Python debugger did not acknowledge attachment.", error);
                            }
                        }
                        if (module is null)
                        {
                            try
                            {
                                using (Py.GIL())
                                {
                                    module = Py.Import(_moduleName);
                                    jsonModule = Py.Import("json");
                                }
                            }
                            catch (Exception error)
                            {
                                throw new PythonCalculationException(PythonCalculationStage.Import,
                                    $"Python module '{_moduleName}' could not be imported.", error);
                            }
                        }
                        try
                        {
                            _debugGate?.WaitForBreakpointAsync(Environment.ProcessId, work.CancellationToken)
                                .GetAwaiter().GetResult();
                            ready = true;
                        }
                        catch (OperationCanceledException) when (work.CancellationToken.IsCancellationRequested) { throw; }
                        catch (Exception error)
                        {
                            throw new PythonCalculationException(PythonCalculationStage.Debugger,
                                "The Python breakpoint did not bind.", error);
                        }
                    }

                    string result;
                    using (Py.GIL())
                    {
                        using PyObject input = work.Json.ToPython();
                        PyObject decoded;
                        try
                        {
                            using PyObject loads = jsonModule!.GetAttr("loads");
                            decoded = loads.Invoke(input);
                        }
                        catch (Exception error)
                        {
                            throw new PythonCalculationException(PythonCalculationStage.Conversion,
                                "The calculation request is not valid JSON.", error);
                        }
                        using (decoded)
                        {
                            PyObject solved;
                            try
                            {
                                using PyObject function = module!.GetAttr("build_analysis_result_set");
                                solved = function.Invoke(decoded);
                            }
                            catch (Exception error)
                            {
                                throw new PythonCalculationException(PythonCalculationStage.Solver,
                                    "Python analysis failed.", error);
                            }
                            using (solved)
                            {
                                try
                                {
                                    using PyObject dumps = jsonModule.GetAttr("dumps");
                                    using PyObject encoded = dumps.Invoke(solved);
                                    result = encoded.As<string>();
                                }
                                catch (Exception error)
                                {
                                    throw new PythonCalculationException(PythonCalculationStage.Conversion,
                                        "Python analysis result could not be serialized.", error);
                                }
                            }
                        }
                    }
                    ValidateCanonicalRoot(result);
                    work.CancellationToken.ThrowIfCancellationRequested();
                    completedJson = result;
                }
                catch (OperationCanceledException) when (work.CancellationToken.IsCancellationRequested)
                {
                    canceled = true;
                }
                catch (Exception error)
                {
                    if (!ready && error is PythonCalculationException startupError &&
                        startupError.Stage is not PythonCalculationStage.Debugger)
                        startupFailure = startupError;
                    failure = error;
                }
                finally
                {
                    Volatile.Write(ref _busy, 0);
                    work.Idle.TrySetResult();
                    if (canceled) work.Completion.TrySetCanceled(work.CancellationToken);
                    else if (failure is not null) work.Completion.TrySetException(failure);
                    else work.Completion.TrySetResult(completedJson!);
                }
            }
        }
        catch (Exception error) { _stopped.TrySetException(error); }
        finally
        {
            try
            {
                if (initialized)
                {
                    // EndAllowThreads and Shutdown must run on the initialization thread.
                    if (threadState != IntPtr.Zero)
                        PythonEngine.EndAllowThreads(threadState);
                    module?.Dispose();
                    jsonModule?.Dispose();
                    PythonEngine.Shutdown();
                }
                _stopped.TrySetResult();
            }
            catch (Exception error) { _stopped.TrySetException(error); }
        }
    }

    private static void ValidateCanonicalRoot(string json)
    {
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("kind", out JsonElement kind) && kind.ValueKind == JsonValueKind.String && kind.GetString() == "analysis_result_set" &&
                root.TryGetProperty("schema_version", out JsonElement version) && version.ValueKind == JsonValueKind.String && version.GetString() == "1.0" &&
                root.TryGetProperty("cases", out JsonElement cases) && cases.ValueKind == JsonValueKind.Array &&
                root.TryGetProperty("results", out JsonElement results) && results.ValueKind == JsonValueKind.Array &&
                root.TryGetProperty("topology", out JsonElement topology) && topology.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("units", out JsonElement units) && units.ValueKind == JsonValueKind.Object)
                return;
        }
        catch (JsonException error)
        {
            throw new PythonCalculationException(PythonCalculationStage.Conversion,
                "Python returned invalid JSON.", error);
        }
        throw new PythonCalculationException(PythonCalculationStage.Conversion,
            "Python did not return an AnalysisResultSet v1 root.");
    }
}
