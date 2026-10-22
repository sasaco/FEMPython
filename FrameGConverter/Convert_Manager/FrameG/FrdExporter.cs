using System;
using System.Collections.Generic;
using Convert_Manager.Lzh;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Convert_Manager.FrameG
{
    /// <summary>
    /// Converts a 2D FrameWeb input JSON (the format FrameWebforCS saves) into a FrameG .frd archive,
    /// the reverse of <see cref="ConvertManager"/>. Input FrameG cannot represent is reported as errors
    /// and produces no archive; information FrameG cannot keep is reported as warnings.
    /// </summary>
    public static class FrdExporter
    {
        public static FrdExportResult Export(string json, FrdExportOptions options = null)
        {
            options = options ?? new FrdExportOptions();
            var diagnostics = new FrdDiagnosticList();
            var empty = new Dictionary<string, int>();

            JObject root;
            try
            {
                root = JObject.Parse(json ?? "", new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            }
            catch (JsonException error)
            {
                diagnostics.Error(FrdDiagnosticCodes.InvalidJson, "", $"JSON として読めません: {error.Message}");
                return new FrdExportResult(null, diagnostics.Items, empty, empty, empty);
            }

            FrdModel model = FrdModelReader.Read(root, options.Title, diagnostics);
            List<LzhEntry> entries = diagnostics.HasErrors ? null : FrdFileWriter.Write(model, diagnostics);
            byte[] frd = diagnostics.HasErrors
                ? null
                : LzhArchiveWriter.Write(entries, options.Time ?? DateTimeOffset.Now);
            return new FrdExportResult(frd, diagnostics.Items, model.NodeNumbers, model.MemberNumbers, model.LoadCaseNumbers);
        }
    }
}
