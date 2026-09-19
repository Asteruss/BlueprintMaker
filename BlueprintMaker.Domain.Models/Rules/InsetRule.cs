using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: вычисляет контур путем отступа внутрь от родительского контура.
/// Используется для Световых пространств.
/// </summary>
public sealed class InsetRule : IDerivationRule
{
    public Contour Resolve(IWindowNode parent)
    {
        double insetWidth = parent.GetPhysicalWidth();

        var parentContour = parent.GetContour();
        var insetVertices = ContourMath.Inset(parentContour.Vertices, insetWidth);
        return Contour.CreateClosed(insetVertices);
    }
}