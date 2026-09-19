using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;

namespace BlueprintMaker.Domain.Models.Elements;

/// <summary>
/// Световое пространство (слот). Форма полностью выводится из родителя.
/// </summary>
public sealed class LightSpace : WindowNodeBase
{
    // Обычное пространство (внутри рамы) делает отступ внутрь
    internal LightSpace(Guid id, IWindowNode parent)
        : base(id, localContour: null, rule: new InsetRule())
    {
    }

    // Пространство, созданное при разрезании импостом
    internal LightSpace(Guid id, IWindowNode parent, Impost impost, SplitSide side)
        : base(id, localContour: null, rule: new SplitHalfRule(impost, side))
    {
    }

    public override double GetPhysicalWidth() => 0;

    /// <summary>
    /// Вставка импоста 
    /// </summary>
    public (LightSpace space1, LightSpace space2, Impost impost) InsertImpost(AxisOrientation orientation, double relativePosition = 0.5)
    {
        if (Children.Count > 0)
            throw new InvalidOperationException("Пространство уже занято. Сначала очистите его.");

        // 1. Создаем импост
        var impost = new Impost(Guid.NewGuid(), this, orientation, relativePosition);
        AddChildInternal(impost);

        // 2. Создаем два новых световых пространства 
        var space1 = new LightSpace(Guid.NewGuid(), this, impost, SplitSide.First);
        var space2 = new LightSpace(Guid.NewGuid(), this, impost, SplitSide.Second);

        AddChildInternal(space1);
        AddChildInternal(space2);

        return (space1, space2, impost);
    }
}