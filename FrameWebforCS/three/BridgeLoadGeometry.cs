using FrameWebforCS.components.input;

namespace FrameWebforCS.three;

/// <summary>Double precision geometry for displaying the solver's arc-length ruled strips.</summary>
internal static class BridgeLoadGeometry
{
    internal sealed record Strip(BridgePoint[] Corners, double S0, double S1,
        double P11, double P12, double P21, double P22)
    {
        internal double Value(double s, double t) =>
            (1 - t) * (P11 + s * (P12 - P11)) + t * (P21 + s * (P22 - P21));
    }

    internal static BridgePoint Add(BridgePoint a, BridgePoint b) => new(a.X+b.X, a.Y+b.Y, a.Z+b.Z);
    internal static BridgePoint Sub(BridgePoint a, BridgePoint b) => new(a.X-b.X, a.Y-b.Y, a.Z-b.Z);
    internal static BridgePoint Scale(BridgePoint a, double s) => new(a.X*s, a.Y*s, a.Z*s);
    internal static double Dot(BridgePoint a, BridgePoint b) => a.X*b.X+a.Y*b.Y+a.Z*b.Z;
    internal static BridgePoint Cross(BridgePoint a, BridgePoint b) =>
        new(a.Y*b.Z-a.Z*b.Y, a.Z*b.X-a.X*b.Z, a.X*b.Y-a.Y*b.X);
    internal static double Length(BridgePoint a) => Math.Sqrt(Dot(a,a));
    internal static BridgePoint Unit(BridgePoint a)
    {
        double length = Length(a);
        if (!double.IsFinite(length) || length <= 0) throw new ArgumentException("方向・面の法線が不正です。");
        return Scale(a, 1/length);
    }
    internal static BridgePoint Lerp(BridgePoint a, BridgePoint b, double t) => Add(a, Scale(Sub(b,a),t));

    internal static double[] Parameters(IReadOnlyList<BridgePoint> points)
    {
        if (points.Count < 2) throw new ArgumentException("載荷ラインには2点以上が必要です。");
        var lengths = new double[points.Count];
        for (int i=1;i<points.Count;i++)
        {
            double segment = Length(Sub(points[i],points[i-1]));
            if (!(segment>0) || !double.IsFinite(segment)) throw new ArgumentException("載荷ラインに重複点があります。");
            lengths[i] = lengths[i-1]+segment;
        }
        if (!double.IsFinite(lengths[^1])) throw new ArgumentException("載荷ラインの長さが不正です。");
        return lengths.Select(x=>x/lengths[^1]).ToArray();
    }

    internal static BridgePoint At(IReadOnlyList<BridgePoint> points, double[] parameters, double s)
    {
        int found = Array.BinarySearch(parameters,s);
        int i = Math.Clamp(found >= 0 ? found : ~found-1,0,points.Count-2);
        return Lerp(points[i],points[i+1],(s-parameters[i])/(parameters[i+1]-parameters[i]));
    }

    internal static IReadOnlyList<Strip> BuildStrips(BridgePath first, BridgePath second, BridgeLoad load,
        double? correspondenceTolerance = null)
    {
        var a=first.Points; var b=second.Points;
        var u=Parameters(a); var v=Parameters(b);
        double direct=Dot(Sub(a[0],b[0]),Sub(a[0],b[0]))+Dot(Sub(a[^1],b[^1]),Sub(a[^1],b[^1]));
        double reverse=Dot(Sub(a[0],b[^1]),Sub(a[0],b[^1]))+Dot(Sub(a[^1],b[0]),Sub(a[^1],b[0]));
        var bounds = a.Concat(b).ToArray();
        double extent = Length(new(bounds.Max(p=>p.X)-bounds.Min(p=>p.X),
            bounds.Max(p=>p.Y)-bounds.Min(p=>p.Y),bounds.Max(p=>p.Z)-bounds.Min(p=>p.Z)));
        if (Math.Abs(direct-reverse)<=(correspondenceTolerance ?? (1e-9+1e-9*extent)*extent))
            throw new ArgumentException("2本の載荷ラインの始終点対応が曖昧です。");
        var q=load.EndIntensities;
        double p21=q[1][0],p22=q[1][1];
        if(reverse<direct)
        {
            b=b.Reverse().ToArray(); v=Parameters(b); (p21,p22)=(p22,p21);
        }
        var knots=u.Concat(v).Distinct().Order().ToArray();
        var result=new List<Strip>();
        for(int i=0;i<knots.Length-1;i++)
        {
            if(knots[i+1]-knots[i]<32*2.220446049250313e-16)continue;
            double s=knots[i],t=knots[i+1];
            result.Add(new([At(a,u,s),At(a,u,t),At(b,v,t),At(b,v,s)],s,t,q[0][0],q[0][1],p21,p22));
        }
        return result;
    }

    internal static BridgePoint[] ClipToTriangle(IReadOnlyList<BridgePoint> subject, BridgePoint[] triangle)
    {
        var normal=Unit(Cross(Sub(triangle[1],triangle[0]),Sub(triangle[2],triangle[0])));
        var polygon=subject.ToList();
        double scale=triangle.Max(p=>Length(Sub(p,triangle[0])));
        double eps=1e-12*scale*scale;
        for(int edge=0;edge<3 && polygon.Count>0;edge++)
        {
            var a=triangle[edge];var b=triangle[(edge+1)%3];var direction=Sub(b,a);
            double Side(BridgePoint p)=>Dot(Cross(direction,Sub(p,a)),normal);
            var input=polygon; polygon=[];
            var previous=input[^1];double previousSide=Side(previous);
            foreach(var current in input)
            {
                double currentSide=Side(current);
                if ((currentSide>=-eps)!=(previousSide>=-eps))
                    polygon.Add(Lerp(previous,current,previousSide/(previousSide-currentSide)));
                if(currentSide>=-eps)polygon.Add(current);
                previous=current;previousSide=currentSide;
            }
        }
        return polygon.ToArray();
    }

    internal static bool Contains(BridgePoint[] triangle,BridgePoint point)
    {
        var a=Sub(triangle[1],triangle[0]);var b=Sub(triangle[2],triangle[0]);var p=Sub(point,triangle[0]);
        double aa=Dot(a,a),ab=Dot(a,b),bb=Dot(b,b),pa=Dot(p,a),pb=Dot(p,b);
        double determinant=aa*bb-ab*ab;
        if(determinant<=0)return false;
        double u=(pa*bb-pb*ab)/determinant,v=(pb*aa-pa*ab)/determinant;
        var residual=Sub(p,Add(Scale(a,u),Scale(b,v)));
        return u>=-1e-8 && v>=-1e-8 && u+v<=1+1e-8 && Length(residual)<=1e-7*Math.Max(1,Math.Sqrt(aa+bb));
    }
}
