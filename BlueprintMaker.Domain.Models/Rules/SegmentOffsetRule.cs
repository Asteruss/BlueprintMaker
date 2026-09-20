using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: повторение формы сегмента элемента, к которому крепится доборник
/// </summary>
public sealed class SegmentOffsetRule : IDerivationRule
{
    public int StartSegment { get; }
    public int SegmentCount { get; }

    public SegmentOffsetRule(int startSegment, int segmentCount)
    {
        StartSegment = startSegment;
        SegmentCount = segmentCount;
    }

    public Contour Resolve(IWindowNode parent)
    {
        var parentContour = parent.GetContour();
        var parentVerts = parentContour.Vertices;

        // Если доборник обхватывает весь родитель, просто берем все вершины
        if (SegmentCount == parentVerts.Count && parentContour.IsClosed)
            return Contour.CreateClosed([.. parentVerts]);
        

        var resultVerts = new List<ContourVertex> { parentVerts[StartSegment] };

        for (int i = 1; i <= SegmentCount; i++)
        {
            int idx = (StartSegment + i) % parentVerts.Count;
            resultVerts.Add(parentVerts[idx]);
        }

        return Contour.CreateOpen(resultVerts);
    }
}