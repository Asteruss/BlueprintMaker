using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>Форма добора: полоса вдоль сегмента контура родителя</summary>
public sealed class JambStripRule : IDerivationRule
{
    private readonly int _segmentIndex;
    private readonly JambSide _side;

    /// <summary>Устанавливается самим Jamb, чтобы правило видело актуальную ширину</summary>
    internal Func<double> WidthProvider { get; set; } = () => GeometryConstants.DisplayWidth;

    public JambStripRule(int segmentIndex, JambSide side)
    {
        _segmentIndex = segmentIndex;
        _side = side;
    }

    public Contour Resolve(IWindowNode parent)
    {
        var contour = parent.GetContour();
        double w = WidthProvider();

        var strip = _side == JambSide.Inner
            ? ContourMath.CutEdgeStrip(contour, _segmentIndex, w).strip
            : ContourMath.ExternalEdgeStrip(contour, _segmentIndex, w);

        return strip ?? contour;   // фолбэк, чтобы дерево не падало
    }
}

/// <summary>Форма остатка света после внутреннего добора</summary>
public sealed class JambRemainderRule : IDerivationRule
{
    private readonly Jamb _jamb;

    public JambRemainderRule(Jamb jamb) => _jamb = jamb;

    public Contour Resolve(IWindowNode parent)
    {
        var contour = parent.GetContour();
        var remainder = ContourMath.CutEdgeStrip(contour, _jamb.SegmentIndex, _jamb.GetPhysicalWidth()).remainder;
        return remainder ?? contour;
    }
}