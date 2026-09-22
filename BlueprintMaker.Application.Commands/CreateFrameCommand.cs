using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models;

namespace BlueprintMaker.Application.Commands;

public sealed class CreateFrameCommand : IUndoableCommand
{
    private readonly Document _document;
    private readonly Contour _contour;
    private Frame? _createdFrame;

    public CreateFrameCommand(Document document, Contour contour)
    {
        _document = document;
        _contour = contour;
    }

    public void Execute()
    {
        _createdFrame = Frame.Create(_contour);
        _document.SetRoot(_createdFrame);
    }

    public void Undo()
    {
        if (_createdFrame != null)
            _document.ClearRoot();
    }
}