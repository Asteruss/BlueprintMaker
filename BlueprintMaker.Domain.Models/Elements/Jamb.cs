using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;

namespace BlueprintMaker.Domain.Models.Elements;

public sealed class Jamb : PhysicalElement
{

    // Конструктор принимает индекс сегмента и их количество
    internal Jamb(Guid id, IWindowNode treeParent, IWindowNode wallParent, int startSegment, int segmentCount, int offsetSign)
    : base(id, localContour: null, rule: new SegmentOffsetRule(wallParent, startSegment, segmentCount, offsetSign))
    {
    }

    // Если пользователь заморозил доборник и начал двигать его узлы,
    // нужно сохранить тип контура (замкнут/открыт), который был до заморозки.
    private bool _isClosedCache;
    internal void SetClosedCache(bool isClosed) => _isClosedCache = isClosed;

    protected override Contour RebuildContour(List<ContourVertex> vertices) =>
        _isClosedCache ? Contour.CreateClosed(EnsureClockwise(vertices)) : Contour.CreateOpen(vertices);
    
}