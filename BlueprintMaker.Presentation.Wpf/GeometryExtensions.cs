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
                    double chord = from.Position.DistanceTo(to.Position);
                    double theta = 4 * System.Math.Atan(from.Bulge);

                    // Модуль радиуса
                    double radius = System.Math.Abs(chord / (2 * System.Math.Sin(theta / 2)));
                    bool isLargeArc = System.Math.Abs(theta) > System.Math.PI;

                    // Bulge > 0 = по часовой (внешняя для рамы). Bulge < 0 = против часовой (внутрь).
                    var sweepDir = from.Bulge > 0 ? SweepDirection.Clockwise : SweepDirection.Counterclockwise;

                    ctx.ArcTo(
                        to.Position.ToWpf(),
                        new System.Windows.Size(radius, radius),
                        0,
                        isLargeArc,
                        sweepDir,
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