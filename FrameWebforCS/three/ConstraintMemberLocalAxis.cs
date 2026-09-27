using THREE;

namespace FrameWebforCS.three;

/// <summary>Legacy ThreeMembersService.localAxis basis, including cg rotation in degrees.</summary>
internal static class ConstraintMemberLocalAxis
{
    internal static (Vector3 X, Vector3 Y, Vector3 Z) Get(Vector3 i, Vector3 j, float cg)
    {
        float dx = j.X - i.X, dy = j.Y - i.Y, dz = j.Z - i.Z;
        float length = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
        if (length <= 0 || !float.IsFinite(length))
            throw new ArgumentException("Member endpoints must have finite, distinct positions.");
        float ll = dx / length, mm = dy / length, nn = dz / length;
        float qq = MathF.Sqrt(ll * ll + mm * mm);
        var x = new Vector3(ll, mm, nn);
        Vector3 y, z;
        if (dx == 0 && dy == 0)
        {
            y = new Vector3(nn, 0, 0);
            z = new Vector3(0, 1, 0);
        }
        else
        {
            y = new Vector3(-mm / qq, ll / qq, 0);
            z = new Vector3(-ll * nn / qq, -mm * nn / qq, qq);
        }
        float angle = cg * MathF.PI / 180;
        float cos = MathF.Cos(angle), sin = MathF.Sin(angle);
        return (x,
            new Vector3(cos * y.X + sin * z.X, cos * y.Y + sin * z.Y, cos * y.Z + sin * z.Z),
            new Vector3(-sin * y.X + cos * z.X, -sin * y.Y + cos * z.Y, -sin * y.Z + cos * z.Z));
    }
}
