using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: повторение формы сегмента элемента, к которому крепится доборник
/// </summary>
public sealed class SegmentOffsetRule : IDerivationRule
{
    private readonly IWindowNode _wallParent;
    private readonly int _offsetSign; // 1 = наружу, -1 = внутрь
    public int StartSegment { get; }
    public int SegmentCount { get; }

    public SegmentOffsetRule(IWindowNode wallParent, int startSegment, int segmentCount, int offsetSign)
    {
        _wallParent = wallParent;
        _offsetSign = offsetSign;
        StartSegment = startSegment;
        SegmentCount = segmentCount;
    }

    public Contour Resolve(IWindowNode parent)
    {
        var wallContour = _wallParent.GetContour();
        var wallVerts = wallContour.Vertices;
        var extractedVerts = new List<ContourVertex>();

        // Вырезаем нужные сегменты из стены
        if (SegmentCount == wallVerts.Count && wallContour.IsClosed)
        {
            extractedVerts.AddRange(wallVerts);
        }
        else
        {
            extractedVerts.Add(wallVerts[StartSegment]);
            for (int i = 1; i <= SegmentCount; i++)
            {
                int idx = (StartSegment + i) % wallVerts.Count;
                extractedVerts.Add(wallVerts[idx]);
            }
        }

        // Вычисляем дистанцию сдвига (ширина стены * знак направления)
        double offsetDistance = _wallParent.GetPhysicalWidth() * _offsetSign;

        if (System.Math.Abs(offsetDistance) > 0.0001)
        {
            extractedVerts = ContourMath.OffsetPolyline(extractedVerts, offsetDistance).ToList();
        }

        return wallContour.IsClosed && SegmentCount == wallVerts.Count
            ? Contour.CreateClosed(extractedVerts)
            : Contour.CreateOpen(extractedVerts);
    }
}