using System.Text.Json;
using FrameWebforCS.components.input;
using FrameWebforCS.providers;
using FrameWebforCS.providers.printing;
using FrameWebforCS.three;
using SingleFormsDemo;
using Xunit;

namespace FrameWebforCS.Tests;

[Collection("DisplacementSingletons")]
public sealed class PrintViewportStateTests
{
    [Fact]
    public void MultipleModes_RestoreOriginalModeCaseScaleAndSelection()
    {
        var routing = AppRoutingModule.Instance;
        var input = InputDataService.Instance;
        try
        {
            routing.NotifyInputMode("node");
            using var fixture = JsonDocument.Parse("""
                {"node":{"1":{"x":0},"2":{"x":2}},
                 "member":{"1":{"ni":"1","nj":"2"}},
                 "load":{"1":{"load_node":[{"row":1,"n":"2","tx":3}]}},
                 "result":{"Case1":{"disg":{"1":{"dx":0.1},"2":{"dx":0.2}}}}}
                """);
            input.JsonDataOpen(fixture.RootElement);
            using var service = new ThreeService(new SceneService());
            service.FlushPending();
            service.SelectNode(1);
            var before = service.CapturePrintState();

            service.ApplyPrintView(new PrintDiagramRequest(0, PrintOption.LoadDiagram,
                "print_load", "1", "load", "Load"));
            Assert.Equal("load", service.GetScaleControl()?.Kind);
            service.ApplyPrintView(new PrintDiagramRequest(1, PrintOption.DisplacementDiagram,
                "disg", "Case1", "disg", "Displacement"));
            Assert.Equal("disg", service.GetScaleControl()?.Kind);
            Assert.True(service.DisplacementCount > 0);

            service.RestorePrintState(before);
            var after = service.CapturePrintState();
            Assert.Equal(before.VisibleMode, after.VisibleMode);
            Assert.Equal(before.LoadDisplayMode, after.LoadDisplayMode);
            Assert.Equal(before.LoadCase, after.LoadCase);
            Assert.Equal(before.ResultMode, after.ResultMode);
            Assert.Equal(before.ResultCase, after.ResultCase);
            Assert.Equal(before.ResultComponent, after.ResultComponent);
            Assert.Equal(before.DisplacementScale, after.DisplacementScale);
            Assert.Equal(before.NodeId, after.NodeId);
            Assert.Equal(before.SelectedKind, after.SelectedKind);
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
            routing.NotifyInputMode("node");
        }
    }

    [Fact]
    public void FailedCase_CanRestoreOriginalSelection()
    {
        var routing = AppRoutingModule.Instance;
        try
        {
            routing.NotifyInputMode("node");
            using var service = new ThreeService(new SceneService());
            service.FlushPending();
            var before = service.CapturePrintState();

            Assert.Throws<InvalidOperationException>(() => service.ApplyPrintView(
                new PrintDiagramRequest(0, PrintOption.SectionDiagram,
                    "fsec", "missing", "fx", "Missing")));

            service.RestorePrintState(before);
            var after = service.CapturePrintState();
            Assert.Equal(before.VisibleMode, after.VisibleMode);
            Assert.Equal(before.ResultMode, after.ResultMode);
            Assert.Equal(before.ResultCase, after.ResultCase);
            Assert.Equal(before.SelectedKind, after.SelectedKind);
        }
        finally { routing.NotifyInputMode("node"); }
    }

    [Fact]
    public void DerivedSectionSamples_AreScopedToPrintCapture()
    {
        var routing = AppRoutingModule.Instance;
        var input = InputDataService.Instance;
        try
        {
            routing.NotifyInputMode("node");
            using var fixture = JsonDocument.Parse("""
                {"node":{"1":{"x":0},"2":{"x":10}},
                 "member":{"1":{"ni":"1","nj":"2"}}}
                """);
            input.JsonDataOpen(fixture.RootElement);
            using var service = new ThreeService(new SceneService());
            service.FlushPending();
            var before = service.CapturePrintState();
            var samples = new[]
            {
                new PrintSectionForcePoint(1, 0, 3, true),
                new PrintSectionForcePoint(1, 10, -2, true)
            };

            service.ApplyPrintView(new PrintDiagramRequest(0,
                PrintOption.CombinedSectionDiagram, "comb_fsec", "Derived1", "fx",
                "Combined force", samples));
            Assert.True(service.SectionForceCount > 0);

            service.RestorePrintState(before);
            var after = service.CapturePrintState();
            Assert.Equal(before.ResultMode, after.ResultMode);
            Assert.Equal(before.ResultCase, after.ResultCase);
            Assert.Equal(before.DerivedState.Cases.Count, after.DerivedState.Cases.Count);
            Assert.Equal(before.DerivedState.Revisions.Count, after.DerivedState.Revisions.Count);
        }
        finally
        {
            using var empty = JsonDocument.Parse("{}");
            input.JsonDataOpen(empty.RootElement);
            routing.NotifyInputMode("node");
        }
    }
}
