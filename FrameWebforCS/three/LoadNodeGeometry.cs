using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

/// <summary>
/// Scene geometry corresponding to JS three-load-point.ts and three-load-moment.ts.
/// The caller owns each returned glyph and must dispose it before replacing a load case.
/// baseScale is JS ThreeLoadService.baseScale() (nodeThree.baseScale * 10), while
/// loadScale is its GUI percentage (0..400, default 100).
/// </summary>
internal static class LoadNodeGeometry
{
    internal const int SelectedColor = 0xAFEEEE;

    internal static LoadNodeGlyph? CreatePoint(Vector3 position, string direction, float value,
        float pMax, float baseScale, float loadScale = 100, float offset = 0)
    {
        if (direction is not ("tx" or "ty" or "tz") || value == 0) return null;
        Validate(position, value, pMax, baseScale, loadScale, offset);
        var axis = direction switch
        {
            "tx" => new Vector3(1, 0, 0),
            "ty" => new Vector3(0, 1, 0),
            _ => new Vector3(0, 0, 1)
        };
        // JS ThreeLoadPoint: uLoad points away from the node for negative loads;
        // ArrowHelper.dir is the opposite. The arrow tip is offset from the node.
        var dir = value < 0 ? axis.MultiplyScalar(-1) : axis;
        float length = MathF.Abs(value) / pMax * 2.5f * Scale(baseScale, loadScale);
        var origin = dir.Clone().MultiplyScalar(-(offset + length));
        int color = AxisColor(direction[1]);
        var arrow = new ArrowHelper(dir, origin, length, Color.Hex(color)) { Name = "arrow" };
        var child = new Group { Name = "child" };
        child.Add(arrow);
        var group = new Group { Name = "group" };
        group.Add(child);
        var root = new Group { Name = "PointLoad" };
        root.Position.Copy(position);
        root.Add(group);
        var offsetDirection = $"g{direction[1]}{(value < 0 ? '+' : '-')}";
        return new LoadNodeGlyph(root, arrow, null, null, color, offsetDirection,
            length, offset + length, position);
    }

    internal static LoadNodeGlyph? CreateMoment(Vector3 position, string direction, float value,
        float mMax, float baseScale, float loadScale = 100)
    {
        if (direction is not ("rx" or "ry" or "rz") || value == 0) return null;
        Validate(position, value, mMax, baseScale, loadScale, 0);
        float radius = MathF.Abs(value) / mMax * Scale(baseScale, loadScale);
        int color = AxisColor(direction[1]);

        // JS ThreeLoadMoment.getArrow: a 300 degree EllipseCurve (13 samples) and
        // a three-sided cone at the start of the arc, transformed as one child.
        var arrowGeometry = new ConeBufferGeometry(0.05f, 0.25f, 3, 1, false);
        var arrowMaterial = new MeshBasicMaterial { Color = Color.Hex(color) };
        var arrow = new Mesh(arrowGeometry, arrowMaterial) { Name = "arrow" };
        arrow.Rotation.X = MathF.PI;
        arrow.Rotation.Z = -MathF.PI / 6;
        arrow.Position.Set(MathF.Cos(MathF.PI / 6), MathF.Sin(MathF.PI / 6), 0);

        var points = new Vector3[13];
        for (int i = 0; i < points.Length; i++)
        {
            float angle = MathF.PI / 6 + i * (5 * MathF.PI / 3) / 12;
            points[i] = new Vector3(MathF.Cos(angle), MathF.Sin(angle), 0);
        }
        var lineMaterial = new LineBasicMaterial { Color = Color.Hex(color) };
        var line = new Line(new BufferGeometry().SetFromPoints(points), lineMaterial) { Name = "line" };
        var child = new Group { Name = "child" };
        child.Add(arrow);
        child.Add(line);
        switch (direction)
        {
            case "rx":
                child.Rotation.Set(value < 0 ? 0 : MathF.PI, MathF.PI / 2, MathF.PI / 2,
                    RotationOrder.YXZ);
                break;
            case "ry":
                if (value < 0) child.Rotation.Set(MathF.PI, 0, MathF.PI);
                child.Rotation.X += MathF.PI / 2;
                break;
            case "rz":
                if (value > 0) child.Rotation.Set(MathF.PI, 0, MathF.PI);
                break;
        }
        child.Scale.Set(radius, radius, radius);
        var group = new Group { Name = "group" };
        group.Add(child);
        var root = new Group { Name = "MomentLoad" };
        root.Position.Copy(position);
        root.Add(group);
        return new LoadNodeGlyph(root, null, arrowMaterial, lineMaterial, color,
            $"rg{direction[1]}", radius, radius, position);
    }

    private static float Scale(float baseScale, float loadScale) => baseScale * loadScale / 100;

    private static int AxisColor(char axis) => axis switch
    {
        'x' => 0xFF0000,
        'y' => 0x00FF00,
        _ => 0x0000FF
    };

    private static void Validate(Vector3 position, float value, float maxLoad,
        float baseScale, float loadScale, float offset)
    {
        ArgumentNullException.ThrowIfNull(position);
        if (!float.IsFinite(position.X) || !float.IsFinite(position.Y) ||
            !float.IsFinite(position.Z) || !float.IsFinite(value) ||
            !float.IsFinite(maxLoad) || maxLoad <= 0 ||
            !float.IsFinite(baseScale) || baseScale < 0 ||
            !float.IsFinite(loadScale) || loadScale < 0 || loadScale > 400 ||
            !float.IsFinite(offset) || offset < 0)
            throw new ArgumentOutOfRangeException(nameof(maxLoad), "Load geometry requires finite coordinates, a positive maximum, and valid scales/offset.");
    }
}

/// <summary>
/// Owns all geometry/materials created for one node-load glyph. Selection changes
/// the original arrow/arc materials, matching JS highlight() without rebuilding.
/// </summary>
internal sealed class LoadNodeGlyph : IDisposable
{
    private readonly ArrowHelper? _pointArrow;
    private readonly MeshBasicMaterial? _momentArrowMaterial;
    private readonly LineBasicMaterial? _momentLineMaterial;
    private readonly int _normalColor;
    private bool _disposed;

    internal LoadNodeGlyph(Group root, ArrowHelper? pointArrow,
        MeshBasicMaterial? momentArrowMaterial, LineBasicMaterial? momentLineMaterial,
        int normalColor, string offsetDirection, float size, float endOffset, Vector3 anchor)
    {
        Root = root;
        _pointArrow = pointArrow;
        _momentArrowMaterial = momentArrowMaterial;
        _momentLineMaterial = momentLineMaterial;
        _normalColor = normalColor;
        OffsetDirection = offsetDirection;
        Size = size;
        EndOffset = endOffset;
        Anchor = anchor.Clone();
    }

    internal Group Root { get; }
    internal string OffsetDirection { get; }
    internal float Size { get; }
    internal float EndOffset { get; }
    internal Vector3 Anchor { get; }

    internal void Highlight(bool selected)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var color = Color.Hex(selected ? LoadNodeGeometry.SelectedColor : _normalColor);
        _pointArrow?.SetColor(color);
        if (_momentArrowMaterial != null) _momentArrowMaterial.Color = color;
        if (_momentLineMaterial != null) _momentLineMaterial.Color = color;
        // JS Point/Moment setText adds a value label only while selected. THREE's
        // C# font geometry has no matching loaded font path; value labels remain
        // a separate UI integration task (three-load-text.ts).
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        DisposeTree(Root);
    }

    private static void DisposeTree(Object3D item)
    {
        foreach (var child in item.Children.ToArray())
        {
            item.Remove(child);
            DisposeTree(child);
        }
        item.Geometry?.Dispose();
        item.Material?.Dispose();
        item.Dispose();
    }
}
