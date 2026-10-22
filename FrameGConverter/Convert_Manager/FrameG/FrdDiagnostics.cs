using System;
using System.Collections.Generic;
using System.Linq;

namespace Convert_Manager.FrameG
{
    public enum FrdDiagnosticSeverity
    {
        Warning,
        Error
    }

    /// <summary>One problem found while exporting; <see cref="Path"/> points into the input JSON.</summary>
    public sealed class FrdDiagnostic
    {
        internal FrdDiagnostic(FrdDiagnosticSeverity severity, string code, string path, string message)
        {
            Severity = severity;
            Code = code;
            Path = path;
            Message = message;
        }

        public FrdDiagnosticSeverity Severity { get; }
        public string Code { get; }
        public string Path { get; }
        public string Message { get; }

        public override string ToString() =>
            Path.Length == 0 ? $"{Code}: {Message}" : $"{Code} {Path}: {Message}";
    }

    /// <summary>Diagnostic codes. Errors stop the export; warnings report information FrameG cannot keep.</summary>
    public static class FrdDiagnosticCodes
    {
        // Errors
        public const string InvalidJson = "FRD_INVALID_JSON";
        public const string Dimension = "FRD_DIMENSION";
        public const string UnknownSection = "FRD_UNKNOWN_SECTION";
        public const string Shell = "FRD_SHELL";
        public const string SpatialLoad = "FRD_SPATIAL_LOAD";
        public const string InvalidId = "FRD_INVALID_ID";
        public const string InvalidValue = "FRD_INVALID_VALUE";
        public const string InvalidText = "FRD_INVALID_TEXT";
        public const string Reference = "FRD_REFERENCE";
        public const string Duplicate = "FRD_DUPLICATE";
        public const string Limit = "FRD_LIMIT";
        public const string FieldOverflow = "FRD_FIELD_OVERFLOW";
        public const string MaterialConflict = "FRD_MATERIAL_CONFLICT";
        public const string MemberGeometry = "FRD_MEMBER_GEOMETRY";
        public const string PileSpring = "FRD_PILE_SPRING";
        public const string ImposedDisplacement = "FRD_IMPOSED_DISPLACEMENT";
        public const string MovingLoad = "FRD_MOVING_LOAD";
        public const string LoadType = "FRD_LOAD_TYPE";

        // Warnings
        public const string Renumbered = "FRD_RENUMBERED";
        public const string ThreeDimensionalFieldDropped = "FRD_3D_FIELD_DROPPED";
        public const string SectionIgnored = "FRD_SECTION_IGNORED";
        public const string TextTruncated = "FRD_TEXT_TRUNCATED";
        public const string PrecisionReduced = "FRD_PRECISION_REDUCED";
        public const string DefineNameDropped = "FRD_DEFINE_NAME_DROPPED";
        public const string PileName = "FRD_PILE_NAME";
    }

    public sealed class FrdExportOptions
    {
        /// <summary>The model title FrameG shows ($11.txt). Longer titles are truncated to 64 bytes.</summary>
        public string Title { get; set; } = "FrameWeb";

        /// <summary>The time written into every archive header; the current time when null.</summary>
        public DateTimeOffset? Time { get; set; }
    }

    public sealed class FrdExportResult
    {
        internal FrdExportResult(byte[] frd, IReadOnlyList<FrdDiagnostic> diagnostics,
            IReadOnlyDictionary<string, int> nodeNumbers, IReadOnlyDictionary<string, int> memberNumbers,
            IReadOnlyDictionary<string, int> loadCaseNumbers)
        {
            Frd = frd;
            Diagnostics = diagnostics;
            NodeNumbers = nodeNumbers;
            MemberNumbers = memberNumbers;
            LoadCaseNumbers = loadCaseNumbers;
        }

        /// <summary>The .frd archive, or null when any diagnostic is an error.</summary>
        public byte[] Frd { get; }

        public bool Succeeded => Frd != null;

        public IReadOnlyList<FrdDiagnostic> Diagnostics { get; }

        public IEnumerable<FrdDiagnostic> Errors => Diagnostics.Where(item => item.Severity == FrdDiagnosticSeverity.Error);

        public IEnumerable<FrdDiagnostic> Warnings => Diagnostics.Where(item => item.Severity == FrdDiagnosticSeverity.Warning);

        /// <summary>FrameWeb node ID to the FrameG node number written.</summary>
        public IReadOnlyDictionary<string, int> NodeNumbers { get; }

        /// <summary>FrameWeb member ID to the FrameG member number written.</summary>
        public IReadOnlyDictionary<string, int> MemberNumbers { get; }

        /// <summary>FrameWeb load case ID to the FrameG load case number written.</summary>
        public IReadOnlyDictionary<string, int> LoadCaseNumbers { get; }
    }

    internal sealed class FrdDiagnosticList
    {
        private readonly List<FrdDiagnostic> items = new List<FrdDiagnostic>();
        private readonly HashSet<string> warned = new HashSet<string>();

        public IReadOnlyList<FrdDiagnostic> Items => items;

        public bool HasErrors => items.Any(item => item.Severity == FrdDiagnosticSeverity.Error);

        public void Error(string code, string path, string message) =>
            items.Add(new FrdDiagnostic(FrdDiagnosticSeverity.Error, code, path, message));

        public void Warning(string code, string path, string message) =>
            items.Add(new FrdDiagnostic(FrdDiagnosticSeverity.Warning, code, path, message));

        /// <summary>Adds a warning once per code and path, however often it is found.</summary>
        public void WarningOnce(string code, string path, string message)
        {
            if (warned.Add(code + "\n" + path))
                Warning(code, path, message);
        }
    }
}
