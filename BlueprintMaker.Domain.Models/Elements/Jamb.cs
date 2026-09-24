using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;

namespace BlueprintMaker.Domain.Models.Elements;

public sealed class Jamb : PhysicalElement
{
    public double Width { get; private set; }

    // Конструктор принимает индекс сегмента и их количество
    internal Jamb(Guid id, IWindowNode treeParent, IWindowNode wallParent, int startSegment, int segmentCount, int offsetSign)
    : base(id, localContour: null, rule: new SegmentOffsetRule(wallParent, startSegment, segmentCount, offsetSign))
    {
    }

    public override double GetPhysicalWidth()
    {
        return Width == 0 ? GeometryConstants.DisplayWidth : Width;
    }

    public override void Materialize(double width)
    {
        if (width <= 0) throw new ArgumentException("Ширина должна быть больше нуля.");
        Width = width;
    }

    // Если пользователь заморозил доборник и начал двигать его узлы,
    // нужно сохранить тип контура (замкнут/открыт), который был до заморозки.
    private bool _isClosedCache;
    internal void SetClosedCache(bool isClosed) => _isClosedCache = isClosed;

    protected override Contour RebuildContour(List<ContourVertex> vertices) =>
        _isClosedCache ? Contour.CreateClosed(EnsureClockwise(vertices)) : Contour.CreateOpen(vertices);
    
}