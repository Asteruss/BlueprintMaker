using BlueprintMaker.Core.Geometry.Heplers;

namespace BlueprintMaker.Core.Geometry;

/// <summary>
/// Операции над готовым контуром: отступы, пересечения, габариты, разрезы.
/// Bulge вершины относится к сегменту от неё к следующей вершине.
/// Поддерживаются прямые и дуговые сегменты.
/// </summary>
public static class ContourMath
{
    public static Rect CalculateBounds(IReadOnlyList<ContourVertex> vertices, bool isClosed)
    {
        if (vertices.Count == 0) return default;

        var bounds = Rect.FromPoint(vertices[0].Position);
        double[] extremes = { 0, Math.PI / 2, Math.PI, -Math.PI / 2 };

        for (int i = 0; i < vertices.Count; i++)
        {
            bounds = bounds.Union(vertices[i].Position);

            bool isLast = i == vertices.Count - 1;
            if (isLast && !isClosed) continue;

            var next = isLast ? vertices[0] : vertices[i + 1];
            var seg = new ContourSegment(vertices[i].Position, next.Position, vertices[i].Bulge);
            if (!seg.IsArc) continue;

            foreach (var a in extremes)
            {
                if (!ContourSegment.AngleInSweep(seg.StartAngle, seg.Sweep, a)) continue;
                bounds = bounds.Union(new Point(
                    seg.Center.X + seg.R * Math.Cos(a),
                    seg.Center.Y + seg.R * Math.Sin(a)));
            }
        }
        return bounds;
    }

    /// <summary>
    /// Знаковая площадь замкнутого контура с учётом дуг
    /// (положительная = против часовой в математических осях).
    /// </summary>
    public static double CalculateSignedArea(IReadOnlyList<ContourVertex> vertices)
    {
        int n = vertices.Count;
        double sum = 0;
        for (int i = 0; i < n; i++)
        {
            var a = vertices[i].Position;
            var b = vertices[(i + 1) % n].Position;
            sum += a.X * b.Y - b.X * a.Y;
        }
        double area = sum / 2;

        // Добавка кругового сегмента между хордой и дугой: r²/2 * (θ - sinθ), знак берётся из θ
        for (int i = 0; i < n; i++)
        {
            var seg = new ContourSegment(vertices[i].Position, vertices[(i + 1) % n].Position, vertices[i].Bulge);
            if (!seg.IsArc) continue;
            area += 0.5 * seg.R * seg.R * (seg.Sweep - Math.Sin(seg.Sweep));
        }
        return area;
    }

    public static List<ContourVertex> EnsureClockwise(List<ContourVertex> vertices)
    {
        if (vertices.Count >= 2 && CalculateSignedArea(vertices) < 0)
            PolylineUtils.ReverseClosed(vertices);
        return vertices;
    }

    /// <summary>
    /// Отступ внутрь на заданную ширину. Прямые сдвигаются параллельно,
    /// дуги — концентрически, вершины — пересечение смещённых кривых.
    /// </summary>
    public static IReadOnlyList<ContourVertex> Inset(IReadOnlyList<ContourVertex> vertices, double width)
    {
        int n = vertices.Count;
        bool hasArc = vertices.Any(v => v.IsArcStart);
        if (!ContourRules.IsValidClosed(vertices))
            throw new ArgumentException("Inset требует замкнутый контур.");

        double area = CalculateSignedArea(vertices);
        double d = width * (area < 0 ? -1.0 : 1.0);   // смещение влево по ходу = внутрь

        var segs = SegmentMath.BuildSegs(vertices, true);
        var pts = new Point[n];
        for (int i = 0; i < n; i++)
            pts[i] = SegmentMath.OffsetJoint(segs[(i - 1 + n) % n], segs[i], d);

        var result = new ContourVertex[n];
        for (int i = 0; i < n; i++)
        {
            double bulge = segs[i].IsArc
                ? SegmentMath.SubBulge(segs[i], pts[i], pts[(i + 1) % n], GeometryConstants.MaxOffsetSweep)
                : vertices[i].Bulge;
            result[i] = new ContourVertex(pts[i], bulge);
        }
        return result;
    }

    /// <summary>
    /// Находит точки пересечения контура с осевой линией (вертикальной или горизонтальной).
    /// </summary>
    public static IReadOnlyList<Point> IntersectWithLine(
        IReadOnlyList<ContourVertex> vertices, bool isClosed, AxisOrientation orientation, double coordinate)
        => AxisIntersector.Intersect(vertices, isClosed, orientation, coordinate);

    /// <summary>
    /// Разрезает замкнутый контур открытой линией (импостом) на две половины.
    /// </summary>
    public static (Contour? first, Contour? second) SliceContour(Contour parent, Contour cutter)
    {
        if (cutter.Vertices.Count < 2) return (null, null);

        var ordered = ContourSlicer.OrderCutter(parent.Vertices, cutter.Vertices);
        if (ordered == null) return (null, null);

        return ContourSlicer.Slice(parent.Vertices, ordered);
    }

    /// <summary>
    /// Разрезает контур импостом шириной offsetA + offsetB: первая часть отступает от оси на offsetA,
    /// вторая на offsetB. Края — точное смещение оси (прямые и дуги), обрезанное по контуру родителя.
    /// first — часть с отрицательной стороны оси, second — с положительной.
    /// </summary>
    public static (Contour? first, Contour? second) SliceContourWithOffset(
        Contour parent, Contour cutter, double offsetA, double offsetB)
    {
        if (cutter.Vertices.Count < 2) return (null, null);

        bool hasOffset = Math.Abs(offsetA) > 0.0001 || Math.Abs(offsetB) > 0.0001;
        if (!hasOffset) return SliceContour(parent, cutter);

        var pv = parent.Vertices;

        var ordered = ContourSlicer.OrderCutter(pv, cutter.Vertices);
        if (ordered == null) return (null, null);

        // Продлеваем ось (по касательной на концах) и строим точные смещённые линии
        var ext = ExtendPolyline(ordered, GeometryConstants.ExtendDistance);
        var lineA = PolylineOffset.Offset(ext, -offsetA);   // сторона первой части
        var lineB = PolylineOffset.Offset(ext, offsetB);    // сторона второй части

        var a = PolylineOffset.TrimToParent(lineA, pv);
        var b = PolylineOffset.TrimToParent(lineB, pv);
        if (a == null || b == null) return (null, null);

        var (aF, aB) = ContourSlicer.Slice(pv, a);
        var (bF, bB) = ContourSlicer.Slice(pv, b);
        if (aF == null || aB == null || bF == null || bB == null) return (null, null);

        // Часть «вперёд по родителю» лежит справа от ломаной, если родитель идёт против часовой, иначе слева.
        // Отрицательное смещение (-offsetA) — справа, положительное (+offsetB) — слева.
        bool ccw = CalculateSignedArea(pv) >= 0;
        var first = ccw ? aF : aB;
        var second = ccw ? bB : bF;
        return (first, second);
    }

    /// <summary>
    /// Точное смещение открытой ломаной с дугами на d (d &gt; 0 — влево по ходу).
    /// </summary>
    public static List<ContourVertex> ExactOffset(IReadOnlyList<ContourVertex> verts, double d)
        => PolylineOffset.Offset(verts, d);

    /// <summary>Перегрузка для ломаной из одних прямых.</summary>
    public static List<Point> ExactOffset(IReadOnlyList<Point> pts, double d)
    {
        var res = PolylineOffset.Offset(pts.Select(p => new ContourVertex(p)).ToList(), d);
        return res.Select(v => v.Position).ToList();
    }

    /// <summary>
    /// Смещение ломаной на distance (положительное — вправо по ходу).
    /// Точное смещение, дуги остаются дугами.
    /// </summary>
    public static List<ContourVertex> OffsetPolyline(IReadOnlyList<ContourVertex> vertices, double distance)
    {
        if (vertices.Count < 2) return new List<ContourVertex>(vertices);
        return PolylineOffset.Offset(vertices, -distance);
    }

    /// <summary>
    /// Создаёт замкнутую полосу (брус) вокруг открытого контура (центральной линии).
    /// Расширяется в обе стороны на width/2 теми же точными смещениями, что и SliceContourWithOffset,
    /// поэтому доска и края света совпадают.
    /// </summary>
    public static IReadOnlyList<ContourVertex> OffsetBandCenter(IReadOnlyList<ContourVertex> vertices, double width)
    {
        double half = width / 2.0;
        var left = PolylineOffset.Offset(vertices, -half);
        var right = PolylineOffset.Offset(vertices, half);

        var result = new List<ContourVertex>(left.Count * 2);
        result.AddRange(left);                       // последняя вершина left — торец, Bulge = 0
        for (int k = right.Count - 1; k >= 0; k--)   // правая сторона в обратном порядке
            result.Add(new ContourVertex(right[k].Position, k > 0 ? -right[k - 1].Bulge : 0));
        return result;
    }

    /// <summary>
    /// Доска импоста внутри родителя: края — смещённые линии оси, обрезанные по родителю,
    /// торцы идут по границе родителя (прямой и дуговой). Пустой список, если построить не удалось.
    /// Замена связки OffsetBandCenter + ClipPolygon для контуров с дугами.
    /// </summary>
    public static IReadOnlyList<ContourVertex> ClipBandToContour(
        IReadOnlyList<ContourVertex> centerLine, double width, IReadOnlyList<ContourVertex> parent)
    {
        var empty = new List<ContourVertex>();
        if (width <= 0 || parent.Count < 2) return empty;

        var center = PolylineUtils.DedupOpen(centerLine);
        if (center.Count < 2) return empty;

        var ext = ExtendPolyline(center, GeometryConstants.ExtendDistance);
        var left = PolylineOffset.TrimToParent(PolylineOffset.Offset(ext, -width / 2), parent);
        var right = PolylineOffset.TrimToParent(PolylineOffset.Offset(ext, width / 2), parent);
        if (left == null || right == null) return empty;

        var segs = SegmentMath.BuildSegs(parent, true);
        var lStart = left[0].Position;
        var lEnd = left[left.Count - 1].Position;
        var rStart = right[0].Position;
        var rEnd = right[right.Count - 1].Position;

        var hLs = BoundaryWalker.FindHit(segs, lStart);
        var hLe = BoundaryWalker.FindHit(segs, lEnd);
        var hRs = BoundaryWalker.FindHit(segs, rStart);
        var hRe = BoundaryWalker.FindHit(segs, rEnd);
        if (!hLs.HasValue || !hLe.HasValue || !hRs.HasValue || !hRe.HasValue) return empty;

        var res = new List<ContourVertex>();

        // Левый край вперёд (без последней точки — её даст граница)
        for (int i = 0; i < left.Count - 1; i++) res.Add(left[i]);
        // Торец на конце: по границе родителя от конца левого края к концу правого
        res.AddRange(BoundaryWalker.BoundaryPath(segs, hLe.Value, lEnd, hRe.Value, rEnd));
        // Правый край назад (без последней точки — её даст граница)
        var rRev = PolylineUtils.ReverseOpen(right);
        for (int i = 0; i < rRev.Count - 1; i++) res.Add(rRev[i]);
        // Торец в начале: по границе от начала правого края к началу левого
        res.AddRange(BoundaryWalker.BoundaryPath(segs, hRs.Value, rStart, hLs.Value, lStart));

        return PolylineUtils.CleanClosed(res);
    }

    /// <summary>
    /// Отсекает часть многоугольника subject, которая выходит за пределы выпуклого многоугольника clipWindow.
    /// Дуги не поддерживает (работает по хордам): для контуров с дугами используйте ClipBandToContour.
    /// </summary>
    public static IReadOnlyList<ContourVertex> ClipPolygon(
        IReadOnlyList<ContourVertex> subject, IReadOnlyList<ContourVertex> clipWindow)
        => PolygonClipper.Clip(subject, clipWindow);

    /// <summary>
    /// Продлевает ломаную на концах по касательной. Для прямого концевого сегмента двигает конечную точку,
    /// для дугового добавляет прямой отрезок-касательную (иначе дуга бы исказилась).
    /// </summary>
    public static IReadOnlyList<ContourVertex> ExtendPolyline(IReadOnlyList<ContourVertex> verts, double extendDistance)
    {
        if (verts.Count < 2 || extendDistance <= 0) return verts;

        var result = new List<ContourVertex>(verts);
        int m = verts.Count - 1;

        // Конец
        var lastSeg = new ContourSegment(verts[m - 1].Position, verts[m].Position, verts[m - 1].Bulge);
        var last = verts[m].Position;
        var tEnd = lastSeg.Tangent(last);
        var newLast = new Point(last.X + tEnd.X * extendDistance, last.Y + tEnd.Y * extendDistance);
        if (lastSeg.IsArc)
        {
            result[m] = new ContourVertex(last, 0);
            result.Add(new ContourVertex(newLast, 0));
        }
        else if (verts[m - 1].Position.DistanceTo(last) > 0.001)
        {
            result[m] = new ContourVertex(newLast, verts[m].Bulge);
        }

        // Начало
        var firstSeg = new ContourSegment(verts[0].Position, verts[1].Position, verts[0].Bulge);
        var first = verts[0].Position;
        var tStart = firstSeg.Tangent(first);
        var newFirst = new Point(first.X - tStart.X * extendDistance, first.Y - tStart.Y * extendDistance);
        if (firstSeg.IsArc)
        {
            result.Insert(0, new ContourVertex(newFirst, 0));
        }
        else if (first.DistanceTo(verts[1].Position) > 0.001)
        {
            result[0] = new ContourVertex(newFirst, verts[0].Bulge);
        }

        return result;
    }

    /// <summary>
    /// Пересечение двух кривых (прямая или дуга, заданные как сегмент a1-a2 с bulgeA и b1-b2 с bulgeB),
    /// продлённых до бесконечности. Возвращает точку, ближайшую к near. null — не пересекаются
    /// </summary>
    public static Point? IntersectCurves(
        Point a1, Point a2, double bulgeA,
        Point b1, Point b2, double bulgeB,
        Point near)
    {
        var ca = CarrierMath.Make(new ContourSegment(a1, a2, bulgeA), 0);
        var cb = CarrierMath.Make(new ContourSegment(b1, b2, bulgeB), 0);
        if (!ca.HasValue || !cb.HasValue) return null;

        Point? best = null;
        double bd = double.MaxValue;
        foreach (var p in CarrierMath.Intersect(ca.Value, cb.Value))
        {
            double d = p.DistanceTo(near);
            if (d < bd) { bd = d; best = p; }
        }
        return best;
    }

    /// <summary>
    /// Bulge той же дуги (сегмент a-b с bulge) между точками from и to, лежащими на её окружности.
    /// Точки могут выходить за исходные концы (дуга удлиняется) или лежать внутри (укорачивается)
    /// Для прямой возвращает 0
    /// </summary>
    public static double SubBulge(Point a, Point b, double bulge, Point from, Point to)
    {
        var s = new ContourSegment(a, b, bulge);
        if (!s.IsArc) return 0;
        if (from.DistanceTo(to) < GeometryConstants.Tolerance) return 0;

        double a1 = Math.Atan2(from.Y - s.Center.Y, from.X - s.Center.X);
        double a2 = Math.Atan2(to.Y - s.Center.Y, to.X - s.Center.X);
        double d = (a2 - a1) * s.Sign;
        d %= GeometryConstants.TwoPi;
        if (d < 0) d += GeometryConstants.TwoPi;

        double abs = Math.Abs(s.Sweep);
        if (d > abs + (GeometryConstants.TwoPi - abs) / 2) d -= GeometryConstants.TwoPi;   // небольшой «откат» назад

        return Math.Tan(d * s.Sign / 4.0);
    }
    /// <summary>
    /// Добор снаружи: полоса шириной width вдоль сегмента, наружу от контура
    /// Торцы идут по продолжению соседних сегментов родителя
    /// </summary>
    public static Contour? ExternalEdgeStrip(Contour parent, int segmentIndex, double width)
    {
        var pv = parent.Vertices;
        int n = pv.Count;
        if (width <= 0 || n < 2 || segmentIndex < 0 || segmentIndex >= n) return null;

        var segs = SegmentMath.BuildSegs(pv, true);
        var s = segs[segmentIndex];
        if (s.A.DistanceTo(s.B) < 0.01) return null;

        double d = CalculateSignedArea(pv) < 0 ? width : -width;   // наружу (инверсия Inset)

        var outer = CarrierMath.Make(s, d);
        if (!outer.HasValue) return null;

        var startOuter = SegmentMath.Shift(s.A, s.LeftNormal(s.A), d);
        var endOuter = SegmentMath.Shift(s.B, s.LeftNormal(s.B), d);

        var cPrev = CarrierMath.Make(segs[(segmentIndex - 1 + n) % n], 0);
        var cNext = CarrierMath.Make(segs[(segmentIndex + 1) % n], 0);
        if (cPrev.HasValue) startOuter = Nearest(CarrierMath.Intersect(outer.Value, cPrev.Value), startOuter);
        if (cNext.HasValue) endOuter = Nearest(CarrierMath.Intersect(outer.Value, cNext.Value), endOuter);

        double ob = SubBulge(s.A, s.B, s.Bulge, startOuter, endOuter);

        var verts = new List<ContourVertex>
    {
        new(s.A, s.Bulge),
        new(s.B, 0),
        new(endOuter, -ob),
        new(startOuter, 0),
    };
        return PolylineUtils.MakeContour(verts);
    }

    private static Point Nearest(List<Point> candidates, Point near)
    {
        var best = near;
        double bd = double.MaxValue;
        foreach (var p in candidates)
        {
            double dist = p.DistanceTo(near);
            if (dist < bd) { bd = dist; best = p; }
        }
        return best;
    }

    /// <summary>Длина самой длинной кромки контура (дуги считаются по длине дуги)</summary>
    public static double LongestEdgeLength(Contour c)
    {
        double max = 0;
        foreach (var s in SegmentMath.BuildSegs(c.Vertices, true))
        {
            double len = s.IsArc ? s.R * Math.Abs(s.Sweep) : s.A.DistanceTo(s.B);
            if (len > max) max = len;
        }
        return max;
    }
    /// <summary>
    /// Отрезает от контура полосу шириной width вдоль сегмента segmentIndex.
    /// strip — добор (содержит сам сегмент), remainder — остаток света.
    /// Края полосы обрезаются по соседним сегментам родителя.
    /// </summary>
    public static (Contour? strip, Contour? remainder) CutEdgeStrip(Contour parent, int segmentIndex, double width)
    {
        var pv = parent.Vertices;
        int n = pv.Count;
        if (width <= 0 || segmentIndex < 0 || segmentIndex >= n) return (null, null);

        var a = pv[segmentIndex];
        var b = pv[(segmentIndex + 1) % n];
        if (a.Position.DistanceTo(b.Position) < 0.01) return (null, null);

        // Внутрь = влево по ходу при положительной площади (та же конвенция, что в Inset)
        double d = CalculateSignedArea(pv) < 0 ? -width : width;

        var allSegs = SegmentMath.BuildSegs(pv, true);
        if (allSegs[segmentIndex].IsArc)
            return CutArcStrip(parent, allSegs, segmentIndex, d);

        var edge = new List<ContourVertex> { new(a.Position, a.Bulge), new(b.Position, 0) };
        var ext = ExtendPolyline(edge, GeometryConstants.ExtendDistance);
        var offsetLine = PolylineOffset.Offset(ext, d);

        var trimmed = PolylineOffset.TrimToParent(offsetLine, pv);
        if (trimmed == null) return (null, null);

        var (f, bk) = ContourSlicer.Slice(pv, trimmed);
        if (f == null || bk == null) return (null, null);

        // Добор — та часть, на границе которой лежит исходный сегмент
        var mid = GetSegmentAnchor(parent, segmentIndex).Position;
        return LiesOnBoundary(f, mid) ? (f, bk) : (bk, f);
    }

    /// <summary>Середина сегмента и единичная нормаль внутрь контура (для стрелок создания добора).</summary>
    public static (Point Position, Point Inward) GetSegmentAnchor(Contour contour, int segmentIndex)
    {
        var v = contour.Vertices;
        int n = v.Count;
        var seg = new ContourSegment(v[segmentIndex].Position, v[(segmentIndex + 1) % n].Position, v[segmentIndex].Bulge);

        Point mid;
        if (seg.IsArc)
        {
            double ang = seg.StartAngle + seg.Sweep / 2;
            mid = new Point(seg.Center.X + seg.R * Math.Cos(ang), seg.Center.Y + seg.R * Math.Sin(ang));
        }
        else
        {
            mid = new Point((seg.A.X + seg.B.X) / 2, (seg.A.Y + seg.B.Y) / 2);
        }

        var left = seg.LeftNormal(mid);
        bool ccw = CalculateSignedArea(v) >= 0;
        return (mid, ccw ? left : new Point(-left.X, -left.Y));
    }

    private static bool LiesOnBoundary(Contour c, Point p)
    {
        foreach (var s in SegmentMath.BuildSegs(c.Vertices, true))
            if (s.DistanceTo(p, out _) < 0.01) return true;
        return false;
    }

    /// <summary>
    /// Добор вдоль дуги: края идут по соседям, только пока те реально пересекают смещённую дугу,
    /// иначе — торцевая черта от конца дуги до её смещённой точки.
    /// </summary>
    private static (Contour? strip, Contour? remainder) CutArcStrip(
        Contour parent, ContourSegment[] segs, int idx, double d)
    {
        int n = segs.Length;
        var s = segs[idx];

        var carrier = CarrierMath.Make(s, d);
        if (!carrier.HasValue) return (null, null);

        var aOff = SegmentMath.Shift(s.A, s.LeftNormal(s.A), d);
        var bOff = SegmentMath.Shift(s.B, s.LeftNormal(s.B), d);

        Point? startHit = NeighborHit(carrier.Value, segs[(idx - 1 + n) % n], aOff);
        Point? endHit = NeighborHit(carrier.Value, segs[(idx + 1) % n], bOff);

        Point sp = startHit ?? aOff;
        Point ep = endHit ?? bOff;
        if (sp.DistanceTo(ep) < 0.01) return (null, null);

        double bulge = SubBulge(s.A, s.B, s.Bulge, sp, ep);

        var cutter = new List<ContourVertex>();
        if (!startHit.HasValue) cutter.Add(new ContourVertex(s.A, 0));   // торец: A -> A'
        cutter.Add(new ContourVertex(sp, bulge));
        cutter.Add(new ContourVertex(ep, 0));
        if (!endHit.HasValue) cutter.Add(new ContourVertex(s.B, 0));     // торец: B' -> B

        var (f, bk) = ContourSlicer.Slice(parent.Vertices, cutter);
        if (f == null || bk == null) return (null, null);

        var mid = GetSegmentAnchor(parent, idx).Position;
        return LiesOnBoundary(f, mid) ? (f, bk) : (bk, f);
    }

    /// <summary>
    /// Пересечение смещённой кривой с реальным сегментом соседа (не с его продолжением)
    /// Возвращает ближайшую к near точку или null
    /// </summary>
    private static Point? NeighborHit(Carrier offsetCarrier, ContourSegment neighbor, Point near)
    {
        var nc = CarrierMath.Make(neighbor, 0);
        if (!nc.HasValue) return null;

        Point? best = null;
        double bd = double.MaxValue;
        foreach (var p in CarrierMath.Intersect(offsetCarrier, nc.Value))
        {
            double t = neighbor.Param(p);
            if (t < -1e-6 || t > 1 + 1e-6) continue;   // только в пределах сегмента соседа

            double dist = p.DistanceTo(near);
            if (dist < bd) { bd = dist; best = p; }
        }
        return best;
    }
}