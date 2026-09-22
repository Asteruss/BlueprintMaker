using BlueprintMaker.Core.Geometry;
using System.Windows.Media;

namespace BlueprintMaker.Presentation.Wpf;

public static class GeometryExtensions
{
    public static StreamGeometry ToWpfGeometry(this Contour contour)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var vertices = contour.Vertices;
            if (vertices.Count == 0) return geometry;

            ctx.BeginFigure(vertices[0].Position.ToWpf(), contour.IsClosed, contour.IsClosed);

            for (int i = 0; i < vertices.Count; i++)
            {
                if (!contour.IsClosed && i == vertices.Count - 1) break;

                var from = vertices[i];
                var to = contour.IsClosed ? vertices[(i + 1) % vertices.Count] : vertices[i + 1];

                if (System.Math.Abs(from.Bulge) < 0.0001)
                {
                    ctx.LineTo(to.Position.ToWpf(), true, false);
                }
                else
                {
                    var arc = BulgeMath.GetArcParameters(from.Position, to.Position, from.Bulge);
                    ctx.ArcTo(
                        to.Position.ToWpf(),
                        new System.Windows.Size(arc.Radius, arc.Radius),
                        0,
                        System.Math.Abs(arc.SweepAngle) > System.Math.PI,
                        arc.SweepAngle >= 0 ? SweepDirection.Counterclockwise : SweepDirection.Clockwise,
                        true, false);
                }
            }
        }
        geometry.Freeze();
        return geometry;
    }

    public static System.Windows.Point ToWpf(this Point p) => new(p.X, p.Y);
    public static Point ToCore(this System.Windows.Point p) => new(p.X, p.Y);
}