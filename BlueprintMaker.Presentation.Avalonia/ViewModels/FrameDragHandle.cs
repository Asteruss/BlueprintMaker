using Avalonia.Input;
using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

/// <summary>
/// Ручка для перемещения рамы (drag). Появляется в центре bounds рамы
/// </summary>
public partial class FrameDragHandle : SelectableElement
{
    public Frame SourceFrame { get; }

    [Reactive] private double _x;
    [Reactive] private double _y;

    public Cursor DragCursor => new(StandardCursorType.SizeAll);

    public FrameDragHandle(Frame frame, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceFrame = frame;
        UpdatePosition();
        DisplayName = "Перемещение рамы";
    }

    /// <summary>
    /// Обновить позицию ручки (при Refresh, после MoveBy)
    /// </summary>
    public void UpdatePosition()
    {
        var contour = SourceFrame.GetContour();
        var bounds = contour.BoundingBox;

        // Целевая точка — правый верхний угол bounds
        var target = new Point(bounds.MaxX, bounds.MinY);

        // Найти ближайшую вершину к target
        Point closest = target;
        double minDist = double.MaxValue;
        foreach (var v in contour.Vertices)
        {
            var d = v.Position.DistanceTo(target);
            if (d < minDist)
            {
                minDist = d;
                closest = v.Position;
            }
        }

        // Направление от центра bounds к ближайшей вершине (наружу)
        var center = new Point(
            (bounds.MinX + bounds.MaxX) / 2.0,
            (bounds.MinY + bounds.MaxY) / 2.0);
        var dir = new Point(closest.X - center.X, closest.Y - center.Y);
        double len = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
        if (len > 0.001)
        {
            dir = new Point(dir.X / len, dir.Y / len);
        }
        else
        {
            dir = new Point(1, -1);  // fallback — вправо-вверх
            var dirLen = Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
            dir = new Point(dir.X / dirLen, dir.Y / dirLen);
        }

        // Handle = ближайшая вершина + 15px в направлении наружу
        double offset = 15.0;
        _x = closest.X + dir.X * offset;
        _y = closest.Y + dir.Y * offset;
    }

    protected override void OnSelectedChanged() { }
}