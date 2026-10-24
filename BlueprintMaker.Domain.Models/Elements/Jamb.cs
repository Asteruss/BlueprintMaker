using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Domain.Models.Rules;

namespace BlueprintMaker.Domain.Models.Elements;

public enum JambSide { Inner, Outer }

public sealed record JambResult(Jamb Jamb, LightSpace? Remainder);

public sealed class Jamb : PhysicalElement
{
    private readonly JambAssembly _assembly;

    public int SegmentIndex { get; }
    public JambSide Side { get; }

    /// <summary>Доска добора (null, пока не материализован).</summary>
    public Board? Board => _assembly.Board;

    internal Jamb(Guid id, int segmentIndex, JambSide side)
        : this(id, segmentIndex, side, new JambStripRule(segmentIndex, side)) { }

    private Jamb(Guid id, int segmentIndex, JambSide side, JambStripRule rule)
        : base(id, localContour: null, rule: rule)
    {
        SegmentIndex = segmentIndex;
        Side = side;
        rule.WidthProvider = GetPhysicalWidth;
        _assembly = new JambAssembly(this);
    }

    // Заглушка для совместимости с кодом заморозки
    internal void SetClosedCache(bool isClosed) { }

    protected override Contour RebuildContour(List<ContourVertex> vertices) =>
        Contour.CreateClosed(EnsureClockwise(vertices));
}