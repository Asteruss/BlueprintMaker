namespace BlueprintMaker.Core.Geometry.Heplers;

/// <summary>Отсечение многоугольника по выпуклому окну (только прямые сегменты).</summary>
internal static class PolygonClipper
{
    public static IReadOnlyList<ContourVertex> Clip(
        IReadOnlyList<ContourVertex> subject, IReadOnlyList<ContourVertex> clipWindow)
    {
        var outputList = new List<ContourVertex>(subject);

        for (int i = 0; i < clipWindow.Count; i++)
        {
            var clipEdgeStart = clipWindow[i].Position;
            var clipEdgeEnd = clipWindow[(i + 1) % clipWindow.Count].Position;

            var inputList = outputList;
            outputList = new List<ContourVertex>();

            if (inputList.Count == 0) break;

            double dx = clipEdgeEnd.X - clipEdgeStart.X;
            double dy = clipEdgeEnd.Y - clipEdgeStart.Y;
            double nx = -dy;
            double ny = dx;

            for (int j = 0; j < inputList.Count; j++)
            {
                var current = inputList[j];
                var prev = inputList[(j - 1 + inputList.Count) % inputList.Count];

                bool currentInside = IsInside(current.Position, clipEdgeStart, nx, ny);
                bool prevInside = IsInside(prev.Position, clipEdgeStart, nx, ny);

                if (currentInside)
                {
                    if (!prevInside)
                        outputList.Add(new ContourVertex(Intersect(prev.Position, current.Position, clipEdgeStart, clipEdgeEnd), 0));
                    outputList.Add(current);
                }
                else if (prevInside)
                {
                    outputList.Add(new ContourVertex(Intersect(prev.Position, current.Position, clipEdgeStart, clipEdgeEnd), 0));
                }
            }
        }

        return outputList;
    }

    private static bool IsInside(Point test, Point edgeStart, double nx, double ny)
    {
        double px = test.X - edgeStart.X;
        double py = test.Y - edgeStart.Y;
        return (px * nx + py * ny) >= -0.5;
    }

    private static Point Intersect(Point p1, Point p2, Point p3, Point p4)
    {
        double d1x = p2.X - p1.X, d1y = p2.Y - p1.Y;
        double d2x = p4.X - p3.X, d2y = p4.Y - p3.Y;

        double denom = d1x * d2y - d1y * d2x;
        if (Math.Abs(denom) < 1e-9) return p2;

        double t = ((p3.X - p1.X) * d2y - (p3.Y - p1.Y) * d2x) / denom;
        return new Point(p1.X + d1x * t, p1.Y + d1y * t);
    }
}