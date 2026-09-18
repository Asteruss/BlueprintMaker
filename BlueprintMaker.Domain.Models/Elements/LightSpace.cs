using BlueprintMaker.Domain.Models.Rules;

namespace BlueprintMaker.Domain.Models.Elements;

/// <summary>
/// Световое пространство (слот). Форма полностью выводится из родителя.
/// </summary>
public sealed class LightSpace : WindowNodeBase
{
    internal LightSpace(Guid id, IWindowNode parent, double frameWidth)
        : base(id, localContour: null, rule: new InsetRule(frameWidth))
    {
    }

    /// <summary>
    /// Обновляет ширину отступа. Вызывается родителем при материализации
    /// </summary>
    internal void UpdateFrameWidth(double newWidth)
    {
        if (newWidth <= 0) throw new ArgumentException("Ширина должна быть больше нуля.");

        // Просто меняем стратегию на новую с другой шириной
        SetRule(new InsetRule(newWidth));
    }
}