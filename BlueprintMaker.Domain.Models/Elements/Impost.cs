using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;
using System.Data;

namespace BlueprintMaker.Domain.Models.Elements;



public sealed class Impost : PhysicalElement
{
    public double Width { get; private set; }

    // Конструктор без ширины
    internal Impost(Guid id, IWindowNode parent, AxisOrientation orientation, double relativePosition)
        : base(id, localContour: null, rule: new ImpostPlacementRule(orientation, relativePosition))
    {
    }

    // Если не материализован (Width == 0), возвращаем дефолт для UI. Иначе реальную ширин
    public override double GetPhysicalWidth()
    {
        return Width == 0 ? GeometryConstants.DisplayWidth : Width;
    }

    public override void Materialize(double width)
    {
        if (width <= 0) throw new ArgumentException("Ширина должна быть больше нуля.");
        Width = width;
    }
    public void UpdatePosition(double newRelativePosition)
    {
        var orientation = (Rule as ImpostPlacementRule)?.Orientation ?? AxisOrientation.Vertical;
        SetRule(new ImpostPlacementRule(orientation, newRelativePosition));
    }

    protected override Contour RebuildContour(List<ContourVertex> vertices) => Contour.CreateClosed(vertices);

}