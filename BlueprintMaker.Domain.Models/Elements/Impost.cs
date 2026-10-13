using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;
using System.Data;

namespace BlueprintMaker.Domain.Models.Elements;



public sealed class Impost : PhysicalElement
{
    // Конструктор без ширины
    internal Impost(Guid id, IWindowNode parent, AxisOrientation orientation, double relativePosition)
        : base(id, localContour: null, rule: new ImpostPlacementRule(orientation, relativePosition))
    {
    }

    public void UpdatePosition(double newRelativePosition)
    {
        var orientation = (Rule as ImpostPlacementRule)?.Orientation ?? AxisOrientation.Vertical;
        SetRule(new ImpostPlacementRule(orientation, newRelativePosition));
    }

    protected override Contour RebuildContour(List<ContourVertex> vertices) => Contour.CreateOpen(vertices);

}