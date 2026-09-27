using System.IO.Pipes;
using System.Text;

namespace FrameWebforCS.calculation;

/// <summary>Optional development-only attach/import/breakpoint handshake.</summary>
public interface ICalculationDebugGate : IAsyncDisposable
{
    Task WaitForAttachAsync(int processId, CancellationToken cancellationToken);
    Task WaitForBreakpointAsync(int processId, CancellationToken cancellationToken);
}

public sealed class CalculationDebugPipeGate : ICalculationDebugGate
{
    private readonly CancellationTokenSource _dispose = new();
    private readonly TimeSpan _timeout;
    private int _disposed;

    public CalculationDebugPipeGate(TimeSpan? timeout = null) =>
        _timeout = timeout ?? TimeSpan.FromMinutes(5);

    public static string PipeNameForProcess(int processId) => $"FrameWebCalculationDebug-{processId}";

    public Task WaitForAttachAsync(int processId, CancellationToken cancellationToken) =>
        WaitForStageAsync("ATTACH", processId, cancellationToken);

    public Task WaitForBreakpointAsync(int processId, CancellationToken cancellationToken) =>
        WaitForStageAsync("BREAKPOINT", processId, cancellationToken);

    private async Task WaitForStageAsync(string stage, int processId, CancellationToken cancellationToken)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _dispose.Token);
        linked.CancelAfter(_timeout);
        try
        {
            using var pipe = new NamedPipeServerStream(PipeNameForProcess(processId), PipeDirection.InOut,
                1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await pipe.WaitForConnectionAsync(linked.Token).ConfigureAwait(false);
            using var reader = new StreamReader(pipe, Encoding.UTF8, leaveOpen: true);
            using var writer = new StreamWriter(pipe, new UTF8Encoding(false), leaveOpen: true)
            { AutoFlush = true };
            await writer.WriteLineAsync($"READY {stage} {processId}").ConfigureAwait(false);
            string? release = await reader.ReadLineAsync(linked.Token).ConfigureAwait(false);
            if (release != $"RELEASE {stage} {processId}")
                throw new InvalidOperationException($"Invalid {stage} debugger acknowledgement.");
            await writer.WriteLineAsync($"ACK {stage} {processId}").ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && !_dispose.IsCancellationRequested)
        {
            throw new TimeoutException($"Timed out waiting for the {stage} debugger gate.");
        }
    }

    public ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return ValueTask.CompletedTask;
        _dispose.Cancel();
        _dispose.Dispose();
        return ValueTask.CompletedTask;
    }
}
