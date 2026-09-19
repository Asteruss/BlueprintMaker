using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;

namespace BlueprintMaker.Domain.Models.Elements;

/// <summary>
/// Световое пространство (слот). Форма полностью выводится из родителя.
/// </summary>
public sealed class LightSpace : WindowNodeBase
{
    // LightSpace - пустота. Создается с правилом InsetRule, 
    // чтобы динамически вычислять свои границы внутри родительского бруса.
    internal LightSpace(Guid id, IWindowNode parent)
        : base(id, localContour: null, rule: new InsetRule())
    {
    }

    // У LightSpace нет физической ширины. Всегда 0.
    public override double GetPhysicalWidth() => 0;

    /// <summary>
    /// ВСТАВКА ИМПОСТА. 
    /// </summary>
    public (LightSpace space1, LightSpace space2, Impost impost) InsertImpost(AxisOrientation orientation, double relativePosition = 0.5)
    {
        if (Children.Count > 0)
            throw new InvalidOperationException("Пространство уже занято. Сначала очистите его.");

        if (Parent is not WindowNodeBase parentBase)
            throw new InvalidOperationException("Родитель не поддерживает управление деревом.");

        // 1. Удаляем себя из родителя
        parentBase.RemoveChildInternal(this);

        // 2. Создаем импост (без аргументов ширины, он сам вернет DisplayWidth=1 до материализации)
        var impost = new Impost(Guid.NewGuid(), parentBase, orientation, relativePosition);
        parentBase.AddChildInternal(impost);

        // 3. Создаем два новых световых пространства внутри импоста
        var space1 = new LightSpace(Guid.NewGuid(), impost);
        var space2 = new LightSpace(Guid.NewGuid(), impost);

        impost.AddChildInternal(space1);
        impost.AddChildInternal(space2);

        return (space1, space2, impost);
    }
}