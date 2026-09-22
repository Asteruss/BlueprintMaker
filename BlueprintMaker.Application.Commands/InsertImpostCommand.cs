using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Application.Commands;

public sealed class InsertImpostCommand : IUndoableCommand
{
    private readonly LightSpace _targetSpace;
    private readonly AxisOrientation _orientation;
    private readonly double _relativePosition;

    private Impost? _impost;
    private LightSpace? _space1;
    private LightSpace? _space2;

    public InsertImpostCommand(LightSpace targetSpace, AxisOrientation orientation, double relativePosition = 0.5)
    {
        _targetSpace = targetSpace;
        _orientation = orientation;
        _relativePosition = relativePosition;
    }

    public void Execute()
    {
        // Вызываем доменный метод, который возвращает созданные элементы
        (_space1, _space2, _impost) = _targetSpace.InsertImpost(_orientation, _relativePosition);
    }

    public void Undo()
    {
        // Удаляем из дерева все 3 созданных элемента (в обратном порядке не обязательно, 
        // так как они братья, но по логике сначала детей пространства, потом сам импост)
        if (_space1 != null) _targetSpace.RemoveChildInternal(_space1);
        if (_space2 != null) _targetSpace.RemoveChildInternal(_space2);
        if (_impost != null) _targetSpace.RemoveChildInternal(_impost);

        // Сбрасываем ссылки
        _impost = null;
        _space1 = null;
        _space2 = null;
    }
}