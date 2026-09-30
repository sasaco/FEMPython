using System;
using System.Text;
using System.Threading;

namespace PDF_Manager
{
    /// <summary>Direct, serialized entry point for desktop PDF generation.</summary>
    public static class DirectPdfGenerator
    {
        public const int MaxRequestBytes = 64 * 1024 * 1024;
        public const int MaxPages = 1000;
        public const int MaxPdfBytes = 128 * 1024 * 1024;

        private static readonly SemaphoreSlim GenerationGate = new SemaphoreSlim(1, 1);

        public static byte[] Generate(string legacyRootJson, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(legacyRootJson))
                throw new ArgumentException("Print data is empty.", nameof(legacyRootJson));
            if (Encoding.UTF8.GetByteCount(legacyRootJson) > MaxRequestBytes)
                throw new ArgumentOutOfRangeException(nameof(legacyRootJson), "Print data exceeds the request limit.");

            cancellationToken.ThrowIfCancellationRequested();
            GenerationGate.Wait(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var bytes = new PrintInput(legacyRootJson, cancellationToken)
                    .GetPdfBytes(MaxPages, MaxPdfBytes, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return bytes;
            }
            finally { GenerationGate.Release(); }
        }
    }
}
