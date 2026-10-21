using BlueprintMaker.Domain.Models;

namespace BlueprintMaker.Application.Commands;

/// <summary>
/// Материализует элемент (Frame/Impost) с заданной шириной.
/// Undo: если до материализации Width = 0 — Dematerialize,
/// иначе — Materialize с прежней шириной
/// </summary>
public sealed class MaterializeCommand : IUndoableCommand
{
    private readonly PhysicalElement _element;
    private readonly double _width;
    private double _oldWidth;

    public MaterializeCommand(PhysicalElement element, double width)
    {
        _element = element;
        _width = width;
    }

    public void Execute()
    {
        _oldWidth = _element.Width;
        _element.Materialize(_width);
    }

    public void Undo()
    {
        if (_oldWidth > 0)
            _element.Materialize(_oldWidth);
        else
            _element.Dematerialize();
    }
}