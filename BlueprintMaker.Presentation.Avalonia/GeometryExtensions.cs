using Avalonia.Media;
using Avalonia;
using BlueprintMaker.Core.Geometry;
using System;

namespace BlueprintMaker.Presentation.Avalonia;

public static class GeometryExtensions
{
    public static StreamGeometry ToAvaloniaGeometry(this Contour contour)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var vertices = contour.Vertices;
            if (vertices.Count == 0) return geometry;

            ctx.BeginFigure(new global::Avalonia.Point(vertices[0].Position.X, vertices[0].Position.Y), isFilled: contour.IsClosed);

            for (int i = 0; i < vertices.Count; i++)
            {
                if (!contour.IsClosed && i == vertices.Count - 1) break;

                var from = vertices[i];
                var to = contour.IsClosed ? vertices[(i + 1) % vertices.Count] : vertices[i + 1];

                if (Math.Abs(from.Bulge) < 0.0001)
                {
                    ctx.LineTo(new global::Avalonia.Point(to.Position.X, to.Position.Y));
                }
                else
                {
                    double chord = from.Position.DistanceTo(to.Position);
                    double theta = 4 * Math.Atan(from.Bulge);
                    double radius = Math.Abs(chord / (2 * Math.Sin(theta / 2)));
                    bool isLargeArc = Math.Abs(theta) > Math.PI;
                    var sweepDir = from.Bulge > 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;

                    ctx.ArcTo(
                        new global::Avalonia.Point(to.Position.X, to.Position.Y),
                        new Size(radius, radius),
                        0, isLargeArc, sweepDir);
                }
            }
                ctx.EndFigure(isClosed: contour.IsClosed);
        }
        return geometry;
    }

    public static global::Avalonia.Point ToAvaloniaPoint(this BlueprintMaker.Core.Geometry.Point point) => new(point.X, point.Y);
}