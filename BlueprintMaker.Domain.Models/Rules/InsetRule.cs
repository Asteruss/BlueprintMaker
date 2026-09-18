using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: вычисляет контур путем отступа внутрь от родительского контура.
/// Используется для Световых пространств.
/// </summary>
public sealed class InsetRule : IDerivationRule
{
    public double Width { get; }

    public InsetRule(double width)
    {
        if (width <= 0)
            throw new ArgumentException("Ширина бруса должна быть больше нуля.", nameof(width));

        Width = width;
    }

    public Contour Resolve(IWindowNode parent)
    {
        // Получаем контур родителя
        var parentContour = parent.GetContour();

        // Делаем отступ внутрь на ширину бруса
        var insetVertices = ContourMath.Inset(parentContour.Vertices, Width);

        // Возвращаем новый замкнутый контур
        return Contour.CreateClosed(insetVertices);
    }
}