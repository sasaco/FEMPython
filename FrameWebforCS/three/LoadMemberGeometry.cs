using THREE;
using Color = THREE.Color;

namespace FrameWebforCS.three;

/// <summary>
/// Input to the family geometry counterpart of JS three-load-member-point.ts,
/// three-load-member-moment.ts, three-load-distribute.ts, three-load-axial.ts,
/// three-load-torsion.ts and three-load-temperature.ts.
/// BaseScale is nodeThree.baseScale; LoadScale is the legacy 0..400 slider.
/// MaxValue is the case-wide maximum for this family, or zero for a single load.
/// </summary>
internal sealed record MemberLoadGeometryInput(
    Vector3 Ni, Vector3 Nj, int Mark, string Direction,
    float L1, float L2, float P1, float P2, float Cg,
    float BaseScale, float LoadScale, float MaxValue = 0, bool Is3D = true);

/// <summary>
/// Owns every geometry and material under Root. Detach Root from its parent
/// before Dispose. Selection recolors all primitives without rebuilding them.
/// </summary>
internal sealed class MemberLoadGeometry : IDisposable
{
    private const int SelectedColor = 0xAFEEEE;
    private readonly List<(Material Material, int Color, int Selected)> _materials = new();
    private bool _disposed;

    internal Group Root { get; } = new() { Name = "member-load" };
    internal string Family { get; }
    internal Vector3 Anchor { get; }
    internal int PrimitiveCount => _materials.Count;

    internal MemberLoadGeometry(string family, Vector3 anchor)
    {
        Family = family;
        Anchor = anchor.Clone();
    }

    internal void AddLine(string name, IReadOnlyList<Vector3> points, int color)
    {
        if (points.Count < 2) return;
        var geometry = new BufferGeometry().SetFromPoints(points.ToArray());
        var material = new LineBasicMaterial { Color = Color.Hex(color) };
        Root.Add(new Line(geometry, material) { Name = name });
        _materials.Add((material, color, SelectedColor));
    }

    internal void AddFace(string name, IReadOnlyList<Vector3> triangles, int color)
    {
        if (triangles.Count < 3) return;
        var positions = new float[triangles.Count * 3];
        for (int i = 0; i < triangles.Count; i++)
        {
            positions[i * 3] = triangles[i].X;
            positions[i * 3 + 1] = triangles[i].Y;
            positions[i * 3 + 2] = triangles[i].Z;
        }
        var geometry = new BufferGeometry();
        geometry.SetAttribute("position", new BufferAttribute<float>(positions, 3));
        var material = new MeshBasicMaterial
        {
            Color = Color.Hex(color), Side = Constants.DoubleSide,
            Transparent = true, Opacity = 0.3f
        };
        Root.Add(new Mesh(geometry, material) { Name = name });
        _materials.Add((material, color, SelectedColor));
    }

    internal void AddCone(string name, Vector3 tip, Vector3 direction, float size, int color)
    {
        if (size <= 0) return;
        var geometry = new CylinderBufferGeometry(0, size * 0.4f, size, 8);
        var material = new MeshBasicMaterial { Color = Color.Hex(color) };
        var mesh = new Mesh(geometry, material) { Name = name };
        mesh.Position.Copy(tip).AddScaledVector(direction, -size * 0.5f);
        mesh.Quaternion.SetFromUnitVectors(new Vector3(0, 1, 0), direction);
        Root.Add(mesh);
        _materials.Add((material, color, SelectedColor));
    }

    internal void AddCylinder(string name, Vector3 a, Vector3 b,
        float radiusA, float radiusB, int color, bool torsionSector = false)
    {
        var axis = new Vector3().SubVectors(b, a);
        float length = axis.Length();
        if (length <= 1e-6f) return;
        var geometry = torsionSector
            ? new CylinderBufferGeometry(radiusA, radiusB, length, 12, 1, true,
                -MathF.PI / 3, 5 * MathF.PI / 3)
            : new CylinderBufferGeometry(radiusB, radiusA, length, 12);
        var material = new MeshBasicMaterial
        {
            Color = Color.Hex(color), Side = Constants.DoubleSide,
            Transparent = true, Opacity = 0.3f
        };
        var mesh = new Mesh(geometry, material) { Name = name };
        mesh.Position.Copy(a).AddScaledVector(axis, 0.5f);
        // JS torsion mesh starts at L1 with its top radius and rotates +90°
        // around Z, so its cylinder Y axis points toward the member i end.
        var cylinderAxis = axis.Normalize();
        if (torsionSector) cylinderAxis.MultiplyScalar(-1);
        mesh.Quaternion.SetFromUnitVectors(new Vector3(0, 1, 0), cylinderAxis);
        Root.Add(mesh);
        _materials.Add((material, color, torsionSector ? 0xFF00FF : SelectedColor));
    }

    internal void AddArrow(string name, Vector3 tail, Vector3 tip, int color, float scale)
    {
        var direction = new Vector3().SubVectors(tip, tail);
        float length = direction.Length();
        if (length <= 1e-6f) return;
        direction.Normalize();
        AddLine(name + "-shaft", new[] { tail, tip }, color);
        AddCone(name + "-head", tip, direction, MathF.Min(length * 0.3f, scale * 0.25f), color);
    }

    internal void SetSelected(bool selected)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        foreach (var (material, normalColor, selectedColor) in _materials)
            material.Color = Color.Hex(selected ? selectedColor : normalColor);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var child in Root.Children.ToArray())
        {
            Root.Remove(child);
            child.Geometry?.Dispose();
            child.Material?.Dispose();
            child.Dispose();
        }
        Root.Dispose();
        _materials.Clear();
    }
}

internal static class MemberLoadGeometryFactory
{
    private const int Red = 0xFF0000;
    private const int Green = 0x00FF00;
    private const int Blue = 0x0000FF;

    internal static MemberLoadGeometry? Create(MemberLoadGeometryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!float.IsFinite(input.L1) || !float.IsFinite(input.L2) ||
            !float.IsFinite(input.P1) || !float.IsFinite(input.P2) ||
            !float.IsFinite(input.Cg) || !float.IsFinite(input.BaseScale) ||
            !float.IsFinite(input.LoadScale) || !float.IsFinite(input.MaxValue) ||
            input.L1 < 0 || input.L2 < 0 || input.BaseScale < 0 ||
            input.LoadScale < 0 || input.LoadScale > 400 || input.MaxValue < 0)
            return null;

        var member = new Vector3().SubVectors(input.Nj, input.Ni);
        float length = member.Length();
        if (length <= 1e-6f) return null;
        var x = member.Clone().Normalize();
        // JS ThreeMembersService.tMatrix uses global Z as the local-Y reference
        // for every nonvertical member (qq != 0), including steep members.
        // Only DX == DY == 0 uses its separate vertical-member basis.
        var reference = member.X == 0 && member.Y == 0
            ? new Vector3(0, 1, 0) : new Vector3(0, 0, 1);
        var y = new Vector3().CrossVectors(reference, x).Normalize();
        var z = new Vector3().CrossVectors(x, y).Normalize();
        float radians = input.Cg * MathF.PI / 180;
        var localY = y.Clone().MultiplyScalar(MathF.Cos(radians)).AddScaledVector(z, MathF.Sin(radians));
        var localZ = z.Clone().MultiplyScalar(MathF.Cos(radians)).AddScaledVector(y, -MathF.Sin(radians));
        var direction = input.Direction?.ToLowerInvariant() ?? "";
        // Match the create() mode guards in each JS family before generating geometry.
        if (!input.Is3D && (input.Mark switch
            {
                1 => direction is not ("x" or "y" or "gx" or "gy"),
                11 => direction is not ("z" or "gz"),
                2 => direction is not ("x" or "y" or "gx" or "gy"),
                _ => false
            })) return null;
        var force = direction switch
        {
            "x" => x.Clone(), "y" => localY.Clone(), "z" => localZ.Clone(),
            "gx" => new Vector3(1, 0, 0), "gy" => new Vector3(0, 1, 0),
            "gz" => new Vector3(0, 0, 1), "r" => x.Clone(),
            _ => null
        };
        if (input.Mark != 9 && force == null) return null;
        int color = direction is "x" or "gx" or "r" ? Red : direction is "y" or "gy" ? Green : Blue;
        // JS ThreeLoadService.onResize: scale = LoadScale/100 * nodeThree.baseScale*10.
        float scale = input.BaseScale * 10 * input.LoadScale / 100;
        if (scale <= 0) return null;
        float max = input.MaxValue > 0 ? input.MaxValue : MathF.Max(MathF.Abs(input.P1), MathF.Abs(input.P2));
        if (input.Mark == 9 ? input.P1 == 0 : input.P1 == 0 && input.P2 == 0)
            return null;
        var middle = input.Ni.Clone().AddScaledVector(member, 0.5f);

        switch (input.Mark)
        {
            case 1:
            case 11:
            {
                if (input.L1 > length || input.L2 > length) return null;
                var family = input.Mark == 1 ? "member-point" : "member-moment";
                var glyph = new MemberLoadGeometry(family, middle);
                // JS constructors reorder the two i-end distances and their values.
                var stations = new[] { (Distance: input.L1, Value: input.P1),
                    (Distance: input.L2, Value: input.P2) }.OrderBy(v => v.Distance).ToArray();
                for (int i = 0; i < 2; i++)
                {
                    var (distance, value) = stations[i];
                    if (value == 0) continue;
                    var point = input.Ni.Clone().AddScaledVector(x, distance);
                    float magnitude = MathF.Abs(value) / max;
                    if (input.Mark == 1)
                    {
                        // JS ThreeLoadMemberPoint.getLoadArrow points from pLb to pLa.
                        // The local/global axial case uses an offset local-y lane.
                        bool axial = MathF.Abs(force!.Dot(x)) > 0.999f;
                        var lane = axial ? localY : force;
                        var start = point.Clone().AddScaledVector(lane!, axial ? scale * 0.3f : 0);
                        var tip = start.Clone();
                        var tail = start.Clone().AddScaledVector(force!, -MathF.Sign(value) * 2.5f * scale * magnitude);
                        glyph.AddArrow($"arrow{i + 1}", tail, tip, color, scale);
                    }
                    else
                        AddMoment(glyph, $"moment{i + 1}", point, force!, value,
                            magnitude * scale, color, localY, localZ);
                }
                return glyph;
            }
            case 2:
            {
                // JS ThreeLoadDistribute/Axial/Torsion use L2 from the j end.
                if (input.L1 + input.L2 > length) return null;
                var pL1 = input.Ni.Clone().AddScaledVector(x, input.L1);
                var pL2 = input.Nj.Clone().AddScaledVector(x, -input.L2);
                float extent1 = input.P1 / max * scale;
                float extent2 = input.P2 / max * scale;
                if (direction == "r")
                {
                    var glyph = new MemberLoadGeometry("member-torsion", middle);
                    // JS ThreeLoadTorsion.getFace: two open 300-degree tapered sectors.
                    var midpoint = pL1.Clone().AddScaledVector(new Vector3().SubVectors(pL2, pL1),
                        input.P1 * input.P2 < 0 ? MathF.Abs(input.P1) /
                        (MathF.Abs(input.P1) + MathF.Abs(input.P2)) : 0.5f);
                    float midRadius = input.P1 * input.P2 < 0 ? 0 : MathF.Abs((extent1 + extent2) * 0.5f);
                    glyph.AddCylinder("torsion-face1", pL1, midpoint,
                        MathF.Abs(extent1) * 0.5f, midRadius * 0.5f, input.P1 >= 0 ? Red : Blue,
                        torsionSector: true);
                    glyph.AddCylinder("torsion-face2", midpoint, pL2,
                        midRadius * 0.5f, MathF.Abs(extent2) * 0.5f, input.P2 >= 0 ? Red : Blue,
                        torsionSector: true);
                    AddMoment(glyph, "torsion-arrow1", pL1, x, input.P1,
                        MathF.Abs(extent1) * 0.5f, input.P1 >= 0 ? Red : Blue, localY, localZ);
                    AddMoment(glyph, "torsion-arrow2", pL2, x, input.P2,
                        MathF.Abs(extent2) * 0.5f, input.P2 >= 0 ? Red : Blue, localY, localZ);
                    return glyph;
                }
                // JS ThreeLoadAxial.create accepts only local x or a global axis
                // exactly parallel to the member; otherwise ThreeLoadDistribute
                // handles the same mark-2 row.
                bool axial = direction == "x" ||
                    direction == "gx" && input.Ni.Y == input.Nj.Y && input.Ni.Z == input.Nj.Z ||
                    direction == "gy" && input.Ni.X == input.Nj.X && input.Ni.Z == input.Nj.Z ||
                    direction == "gz" && input.Ni.X == input.Nj.X && input.Ni.Y == input.Nj.Y;
                if (axial)
                {
                    var glyph = new MemberLoadGeometry("member-axial", middle);
                    var offset = localY.Clone().MultiplyScalar(scale * 0.1f);
                    var a = pL1.Clone().Add(offset);
                    var b = pL2.Clone().Add(offset);
                    if (input.P1 < 0) (a, b) = (b, a);
                    glyph.AddArrow("axial-arrow", a, b, Red, scale);
                    return glyph;
                }
                else
                {
                    var glyph = new MemberLoadGeometry("member-distributed", middle);
                    var uLoad = force.Clone().MultiplyScalar(-1);
                    var a = pL1.Clone();
                    var d = pL2.Clone();
                    var b = a.Clone().AddScaledVector(uLoad, extent1);
                    var c = d.Clone().AddScaledVector(uLoad, extent2);
                    if (input.P1 * input.P2 < 0)
                    {
                        float fraction = MathF.Abs(input.P1) /
                            (MathF.Abs(input.P1) + MathF.Abs(input.P2));
                        var zero = a.Clone().AddScaledVector(new Vector3().SubVectors(d, a), fraction);
                        glyph.AddFace("distributed-face", new[] { a, b, zero, d, c, zero }, color);
                        glyph.AddLine("distributed-outline", new[] { a, b, zero, c, d }, color);
                    }
                    else
                    {
                        glyph.AddFace("distributed-face", new[] { a, b, c, a, c, d }, color);
                        glyph.AddLine("distributed-outline", new[] { a, b, c, d }, color);
                    }
                    return glyph;
                }
            }
            case 9:
            {
                var glyph = new MemberLoadGeometry("member-temperature", middle);
                // JS ThreeLoadTemperature.uLoad = -localAxis.y; its line starts
                // 0.1*scale away from the member in that direction.
                var offset = localY.Clone().MultiplyScalar(-scale * 0.1f);
                glyph.AddLine("temperature-line", new[]
                {
                    input.Ni.Clone().Add(offset), input.Nj.Clone().Add(offset)
                }, Red);
                return glyph;
            }
            default:
                return null;
        }
    }

    private static void AddMoment(MemberLoadGeometry glyph, string name, Vector3 point,
        Vector3 axis, float value, float radius, int color, Vector3 localY, Vector3 localZ)
    {
        if (value == 0 || radius <= 1e-6f) return;
        var radial = MathF.Abs(axis.Dot(localY)) < 0.9f ? localY.Clone() : localZ.Clone();
        radial.AddScaledVector(axis, -radial.Dot(axis)).Normalize();
        var tangent = new Vector3().CrossVectors(axis, radial).Normalize();
        // JS ThreeLoadMoment.getArrow uses EllipseCurve from pi/6 to 11*pi/6
        // and getPoints(12). The axis-frame orientation remains a C# adapter.
        const int steps = 12;
        var arc = new Vector3[steps + 1];
        float sign = MathF.Sign(value);
        for (int i = 0; i <= steps; i++)
        {
            float angle = MathF.PI / 6 + sign * i * (5 * MathF.PI / 3) / steps;
            arc[i] = point.Clone().AddScaledVector(radial, radius * MathF.Cos(angle))
                .AddScaledVector(tangent, radius * MathF.Sin(angle));
        }
        glyph.AddLine(name + "-arc", arc, color);
        var endDirection = new Vector3().SubVectors(arc[^1], arc[^2]).Normalize();
        glyph.AddCone(name + "-head", arc[^1], endDirection, radius * 0.3f, color);
    }
}
