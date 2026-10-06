using FrameWebforCS.calculation;
using System.Text.Json.Nodes;
using Xunit;

namespace FrameWebforCS.Tests;

public sealed class BridgeLoadAuditTests
{
    private static JsonObject Fixture(string name = "bridge-static")
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "FrameWeb")))
            directory = directory.Parent;
        string root = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
        return JsonNode.Parse(File.ReadAllText(Path.Combine(root, "FrameWeb", "tests", "data",
            "contracts", "positive", name + ".json")))!.AsObject();
    }

    private static JsonObject Audit(JsonObject root) =>
        root["results"]![0]!["diagnostics"]!["spatial_loads"]!.AsObject();

    [Fact]
    public void ActualPythonAssemblyAuditUsesPublicNodesAndImmutableLists()
    {
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(Fixture().ToJsonString());
        var item = Assert.IsType<StaticAnalysisResult>(Assert.Single(result.Results));
        SpatialLoadAudit audit = Assert.IsType<SpatialLoadAudit>(item.Diagnostics.SpatialLoads);
        Assert.Equal(50, audit.Resultant.Z, 10);
        Assert.Equal(50, audit.NodeLoads.Sum(node => node.Force.Z), 10);
        Assert.Equal(audit.Resultant.Z, audit.NodalResultant.Z, 10);
        Assert.Equal(53.33333333333333, audit.Moment.X, 10);
        Assert.Equal(-53.33333333333333, audit.Moment.Y, 10);
        Assert.InRange(audit.ForceError, 0, 1e-10);
        SpatialLoadItemAudit load = Assert.Single(audit.Loads);
        Assert.Equal(1, load.LoadId);
        Assert.Equal(7, load.PanelId);
        Assert.Equal("spatial_area", load.Feature);
        Assert.Equal(2, load.ClippedArea, 10);
        Assert.All(audit.NodeLoads, node => Assert.Contains(result.Topology.Nodes, n => n.NodeId == node.NodeId));
        Assert.Throws<NotSupportedException>(() => ((IList<SpatialNodalLoad>)audit.NodeLoads).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<SpatialLoadItemAudit>)audit.Loads).Clear());
    }

    [Fact]
    public void OptionalAuditDoesNotChangeExistingStaticResults()
    {
        AnalysisResultSet result = AnalysisResultSetJson.Deserialize(Fixture("single-static").ToJsonString());
        Assert.Null(Assert.IsType<StaticAnalysisResult>(Assert.Single(result.Results)).Diagnostics.SpatialLoads);
    }

    [Theory]
    [InlineData("missing-node")]
    [InlineData("duplicate-node")]
    [InlineData("duplicate-load")]
    [InlineData("negative-error")]
    [InlineData("invalid-feature")]
    [InlineData("extra-field")]
    [InlineData("missing-component")]
    [InlineData("null-vector")]
    [InlineData("null-audit")]
    [InlineData("infinite-value")]
    [InlineData("negative-area")]
    public void RejectsMalformedAudit(string mutation)
    {
        JsonObject root = Fixture();
        JsonObject audit = Audit(root);
        switch (mutation)
        {
            case "missing-node": audit["node_loads"]![0]!["node_id"] = "missing"; break;
            case "duplicate-node": audit["node_loads"]!.AsArray().Add(audit["node_loads"]![0]!.DeepClone()); break;
            case "duplicate-load": audit["loads"]!.AsArray().Add(audit["loads"]![0]!.DeepClone()); break;
            case "negative-error": audit["force_error"] = -1; break;
            case "invalid-feature": audit["loads"]![0]!["feature"] = "other"; break;
            case "extra-field": audit["source"] = "untrusted"; break;
            case "missing-component": audit["resultant"]!.AsObject().Remove("z"); break;
            case "null-vector": audit["resultant"] = null; break;
            case "null-audit": root["results"]![0]!["diagnostics"]!["spatial_loads"] = null; break;
            case "infinite-value": audit["resultant"]!["z"] = JsonNode.Parse("1e9999"); break;
            case "negative-area": audit["loads"]![0]!["clipped_area"] = -1; break;
        }
        Assert.Throws<AnalysisContractException>(() => AnalysisResultSetJson.Deserialize(root.ToJsonString()));
    }

    [Theory]
    [InlineData("nonlinear-steps")]
    [InlineData("modal")]
    public void RejectsAuditOnNonstaticStates(string fixture)
    {
        JsonObject root = Fixture(fixture);
        root["results"]![0]!["diagnostics"]!["spatial_loads"] = Audit(Fixture()).DeepClone();
        Assert.Throws<AnalysisContractException>(() => AnalysisResultSetJson.Deserialize(root.ToJsonString()));
    }

    [Fact]
    public void AuditConstructorCopiesSourceCollections()
    {
        Vector3Value zero = new(0, 0, 0);
        List<SpatialNodalLoad> nodes = [new("1", zero)];
        List<SpatialLoadItemAudit> loads = [new(1, 7, "spatial_line", zero, zero, zero, zero, 0, 0, 2, 0)];
        var audit = new SpatialLoadAudit(zero, zero, zero, zero, 0, 0, nodes, loads);
        nodes.Clear();
        loads.Clear();
        Assert.Single(audit.NodeLoads);
        Assert.Single(audit.Loads);
    }
}
