using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Rules;

namespace BlueprintMaker.Application.Commands;

public sealed class UpdateImpostPositionCommand : IUndoableCommand
{
    private readonly Impost _impost;
    private readonly double _oldRelativePosition;
    private readonly AxisOrientation _oldOrientation;
    private readonly double _newRelativePosition;

    public UpdateImpostPositionCommand(Impost impost, double oldRelativePosition, double newRelativePosition)
    {
        _impost = impost;
        _oldRelativePosition = oldRelativePosition;
        _oldOrientation = (impost.Rule as ImpostPlacementRule)?.Orientation ?? AxisOrientation.Vertical;
        _newRelativePosition = newRelativePosition;
    }

    public void Execute()
    {
        _impost.UpdatePosition(_newRelativePosition);
    }

    public void Undo()
    {
        var currentOrientation = (_impost.Rule as ImpostPlacementRule)?.Orientation ?? AxisOrientation.Vertical;
        if (currentOrientation != _oldOrientation)
            _impost.SetRule(new ImpostPlacementRule(_oldOrientation, _oldRelativePosition));
        else
            _impost.UpdatePosition(_oldRelativePosition);
    }
}