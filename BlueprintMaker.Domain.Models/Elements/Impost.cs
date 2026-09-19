using BlueprintMaker.Domain.Models.Rules;
using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Elements;



public sealed class Impost : WindowNodeBase, IMaterializable
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

    public void Materialize(double width)
    {
        if (width <= 0) throw new ArgumentException("Ширина должна быть больше нуля.");
        Width = width;
    }
}