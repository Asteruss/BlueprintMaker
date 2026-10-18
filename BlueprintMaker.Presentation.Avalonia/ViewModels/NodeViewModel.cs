using BlueprintMaker.Application;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class NodeViewModel : ReactiveObject
{
    private readonly DocumentHistory _history;
    private Point _dragStartNodePosition;

    public PhysicalElement Element { get; }
    public int Index { get; }

    [Reactive] private double _x;
    [Reactive] private double _y;

    public ReactiveCommand<Point, Unit> MoveCommand { get; }

    public NodeViewModel(PhysicalElement element, int index, Point pos, DocumentHistory history)
    {
        Element = element;
        Index = index;
        X = pos.X;
        Y = pos.Y;
        _history = history;

        MoveCommand = ReactiveCommand.Create<Point>(finalPos =>
        {
            Element.MoveNode(Index, _dragStartNodePosition);

            _history.Execute(new MoveNodeCommand(Element, Index, finalPos));
        });
    }

    /// <summary>
    /// Вызывается при нажатии мыши по ноде
    /// </summary>
    public void StartDrag()
    {
        _dragStartNodePosition = new Point(X, Y);
    }
    public void DragTo(double newX, double newY)
    {
        X = newX;
        Y = newY;

        Element.MoveNode(Index, new Point(newX, newY));
    }

    /// <summary>
    /// Вызывается при отпускании мыши
    /// </summary>
    public void EndDrag(double finalX, double finalY)
    {
        var finalPoint = new Point(finalX, finalY);
        MoveCommand.Execute(finalPoint).Subscribe();
    }
}
