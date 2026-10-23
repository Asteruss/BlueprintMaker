using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Application.Commands;

/// <summary>
/// Сдвигает раму на (dx, dy) 
/// Undo — сдвигает обратно на (-dx, -dy)
/// </summary>
public sealed class MoveFrameCommand : IUndoableCommand
{
    private readonly Frame _frame;
    private readonly double _dx;
    private readonly double _dy;

    public MoveFrameCommand(Frame frame, double dx, double dy)
    {
        _frame = frame;
        _dx = dx;
        _dy = dy;
    }

    public void Execute() => _frame.MoveBy(_dx, _dy);
    public void Undo() => _frame.MoveBy(-_dx, -_dy);
}