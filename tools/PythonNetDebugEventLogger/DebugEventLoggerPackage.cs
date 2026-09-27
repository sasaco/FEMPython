using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Debugger.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace FrameWeb3.PythonNetDebugEventLogger
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [Guid(PackageGuid)]
    public sealed class DebugEventLoggerPackage : AsyncPackage
    {
        public const string PackageGuid = "7c0e6207-a324-4d6d-81f8-4745bb5fab64";

        private IVsDebugger debugger;
        private DebugEventSink sink;

        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            debugger = await GetServiceAsync(typeof(SVsShellDebugger)) as IVsDebugger;
            if (debugger == null)
            {
                ActivityLog.LogError(nameof(DebugEventLoggerPackage), "SVsShellDebugger is unavailable.");
                return;
            }

            sink = new DebugEventSink();
            int hr = debugger.AdviseDebugEventCallback(sink);
            sink.RecordLifecycle("advise", hr);
            if (ErrorHandler.Failed(hr))
            {
                sink.Dispose();
                sink = null;
                ActivityLog.LogError(nameof(DebugEventLoggerPackage), "AdviseDebugEventCallback failed: 0x" + hr.ToString("X8", CultureInfo.InvariantCulture));
            }
        }

        protected override void Dispose(bool disposing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (disposing && sink != null)
            {
                int hr = debugger?.UnadviseDebugEventCallback(sink) ?? VSConstants.E_FAIL;
                sink.RecordLifecycle("unadvise", hr);
                sink.Dispose();
                sink = null;
            }

            base.Dispose(disposing);
        }
    }

    [ComVisible(true)]
    public sealed class DebugEventSink : IVsDebuggerEvents, IDebugEventCallback2, IDisposable
    {
        private static readonly Guid EngineCreate = typeof(IDebugEngineCreateEvent2).GUID;
        private static readonly Guid ProgramCreate = typeof(IDebugProgramCreateEvent2).GUID;
        private static readonly Guid LoadComplete = typeof(IDebugLoadCompleteEvent2).GUID;
        private readonly object gate = new object();
        private readonly StreamWriter writer;
        private int otherEventCount;

        public DebugEventSink()
        {
            string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FrameWeb3", "PythonNetDebugProbe");
            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "debug-events-" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture) + ".tsv");
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            bool newFile = stream.Length == 0;
            writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            if (newFile)
            {
                writer.WriteLine("timestampUtc\tevent\teventIid\tengineGuid\tprocessId\tprogramGuid\tprogramName\tdetail");
            }
            Record("logger-start", Guid.Empty, Guid.Empty, "", "", "", "");
        }

        public int OnModeChange(DBGMODE mode)
        {
            Record("ModeChange", Guid.Empty, Guid.Empty, "", "", "", mode.ToString());
            return VSConstants.S_OK;
        }

        public int Event(IDebugEngine2 engine, IDebugProcess2 process, IDebugProgram2 program, IDebugThread2 thread, IDebugEvent2 debugEvent, ref Guid eventId, uint attributes)
        {
            try
            {
                string name = eventId == EngineCreate ? "EngineCreate" : eventId == ProgramCreate ? "ProgramCreate" : eventId == LoadComplete ? "LoadComplete" : "Other";
                if (name == "Other" && Interlocked.Increment(ref otherEventCount) > 10000)
                {
                    return VSConstants.S_OK;
                }
                if (name != null)
                {
                    Guid engineId = Guid.Empty;
                    int engineHr = engine?.GetEngineId(out engineId) ?? VSConstants.E_POINTER;

                    IDebugProcess2 effectiveProcess = process;
                    if (effectiveProcess == null && program != null)
                    {
                        program.GetProcess(out effectiveProcess);
                    }

                    string pid = "";
                    int processHr = VSConstants.E_POINTER;
                    if (effectiveProcess != null)
                    {
                        var processIds = new AD_PROCESS_ID[1];
                        processHr = effectiveProcess.GetPhysicalProcessId(processIds);
                        if (ErrorHandler.Succeeded(processHr))
                        {
                            pid = processIds[0].ProcessIdType == 0
                                ? processIds[0].dwProcessId.ToString(CultureInfo.InvariantCulture)
                                : processIds[0].guidProcessId.ToString("D");
                        }
                    }

                    Guid programId = Guid.Empty;
                    string programName = "";
                    string engineName = "";
                    int programHr = VSConstants.E_POINTER;
                    int programEngineHr = VSConstants.E_POINTER;
                    if (program != null)
                    {
                        programHr = program.GetProgramId(out programId);
                        program.GetName(out programName);
                        Guid programEngineId;
                        programEngineHr = program.GetEngineInfo(out engineName, out programEngineId);
                        if (ErrorHandler.Succeeded(programEngineHr) && engineId == Guid.Empty)
                        {
                            engineId = programEngineId;
                        }
                    }

                    string detail = "engineHr=0x" + engineHr.ToString("X8", CultureInfo.InvariantCulture)
                        + ";processHr=0x" + processHr.ToString("X8", CultureInfo.InvariantCulture)
                        + ";programHr=0x" + programHr.ToString("X8", CultureInfo.InvariantCulture)
                        + ";programEngineHr=0x" + programEngineHr.ToString("X8", CultureInfo.InvariantCulture)
                        + ";engineName=" + engineName
                        + ";attributes=0x" + attributes.ToString("X8", CultureInfo.InvariantCulture);
                    Record(name, eventId, engineId, pid, programId.ToString("D"), programName, detail);
                }
            }
            catch (Exception ex)
            {
                Record("logger-error", eventId, Guid.Empty, "", "", "", ex.GetType().Name + ": " + ex.Message);
            }
            finally
            {
                // The debugger passes COM interfaces to managed callbacks. Release each RCW once.
                Release(debugEvent);
                Release(thread);
                Release(program);
                Release(process);
                Release(engine);
            }

            return VSConstants.S_OK;
        }

        public void RecordLifecycle(string name, int hr)
        {
            Record(name, Guid.Empty, Guid.Empty, "", "", "", "hr=0x" + hr.ToString("X8", CultureInfo.InvariantCulture));
        }

        private void Record(string name, Guid eventId, Guid engineId, string pid, string programId, string programName, string detail)
        {
            lock (gate)
            {
                writer.Write(DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                foreach (string field in new[] { name, eventId.ToString("D"), engineId.ToString("D"), pid, programId, programName, detail })
                {
                    writer.Write('\t');
                    writer.Write((field ?? "").Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' '));
                }
                writer.WriteLine();
            }
        }

        private static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value))
            {
                Marshal.ReleaseComObject(value);
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                writer.Dispose();
            }
        }
    }
}
