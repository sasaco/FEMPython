using System.Reflection;
using System.Text;
using System.Text.Json;

namespace FrameWebforCS.Headless;

internal sealed record RunnerMessage(string Level, string Code, string Text, string? Path = null);
internal sealed record RunnerError(string Code, string Message, string Category);
internal sealed record RunnerArtifact(string Kind, string MediaType, string RelativePath, long Bytes, string Sha256);
internal sealed record RunnerEnvelope(
    int ProtocolVersion, string Engine, string EngineVersion, string Readiness, bool Ok,
    string ExecutionStatus, string EngineeringStatus, object Summary,
    IReadOnlyList<RunnerMessage> Messages, IReadOnlyList<RunnerArtifact> Artifacts,
    IReadOnlyList<RunnerError> Errors)
{
    internal static RunnerEnvelope Create(bool ok, object summary,
        IReadOnlyList<RunnerArtifact>? artifacts = null, IReadOnlyList<RunnerMessage>? messages = null,
        RunnerError? error = null) => new(1, "fempython",
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "1.0.0", "experimental", ok,
            ok ? "success" : "failed", ok ? "not_checked" : "not_applicable", summary,
            messages ?? [], artifacts ?? [], error is null ? [] : [error]);
}

internal static class Program
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        // Keep the one protocol writer separate from library diagnostics.
        TextWriter protocol = Console.Out;
        Console.SetOut(Console.Error);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        RunnerEnvelope envelope;
        try
        {
            RunOptions options = RunOptions.Parse(args);
            envelope = await HeadlessCalculation.RunAsync(options, cancellation.Token);
        }
        catch (Exception error)
        {
            RunnerError failure = error switch
            {
                RunnerException known => new(known.Code, known.Message, known.Category),
                OperationCanceledException => new("CANCELED", "Calculation was canceled or exceeded ten minutes.", "canceled"),
                calculation.CalculationRequestException => new("INVALID_INPUT", error.Message, "input"),
                JsonException => new("INVALID_INPUT", error.Message, "input"),
                calculation.PythonCalculationException python => new("PYTHON_" + python.Stage.ToString().ToUpperInvariant(), python.Message, "calculation"),
                calculation.AnalysisContractException => new("INVALID_RESULT", error.Message, "calculation"),
                providers.printing.PrintProjectionException => new("PDF_PROJECTION_FAILED", error.Message, "output"),
                IOException => new("IO_ERROR", error.Message, "io"),
                UnauthorizedAccessException => new("IO_ERROR", error.Message, "io"),
                _ => new("EXECUTION_FAILED", error.Message, "calculation"),
            };
            Console.Error.WriteLine($"{failure.Code}: {error.GetType().Name}: {failure.Message}");
            envelope = RunnerEnvelope.Create(false, new { }, error: failure);
        }
        await protocol.WriteLineAsync(JsonSerializer.Serialize(envelope, JsonOptions));
        await protocol.FlushAsync();
        return envelope.Ok ? 0 : 1;
    }
}
