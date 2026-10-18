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

    public override double GetPhysicalWidth() => GeometryConstants.DisplayWidth;

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

    /// <summary>
    /// Вставляет N импостов, деля пространство на N+1 равных частей.
    /// </summary>
    public void InsertMultipleImposts(int count, AxisOrientation orientation)
    {
        if (Children.Count > 0)
            throw new InvalidOperationException("Пространство уже занято.");
        if (count <= 0) return;

        // Делим текущее пространство: 1 часть уходит влево, (count) частей вправо
        double pos = 1.0 / (count + 1);
        var (space1, space2, impost) = InsertImpost(orientation, pos);

        // Рекурсивно делим оставшееся пространство (space2) на count - 1 частей
        if (count > 1)
        {
            space2.InsertMultipleImposts(count - 1, orientation);
        }
    }

    public Sash InsertSash()
    {
        if (Children.Count > 0)
            throw new InvalidOperationException("Пространство уже занято. Сначала очистите его.");

        var sash = new Sash(Guid.NewGuid(), this);
        AddChildInternal(sash);

        return sash;
    }

}