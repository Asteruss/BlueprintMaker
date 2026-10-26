using Avalonia.Media;
using System;
using System.Reactive;
using AvaloniaPoint = global::Avalonia.Point;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class MainViewModel
{
    private bool _firstCentered;

    /// <summary>Вызывается из code-behind при SizeChanged холста.</summary>
    public void UpdateCanvasSize(double width, double height)
    {
        CanvasWidth = width;
        CanvasHeight = height;

        if (_firstCentered || _document.Root == null || Frames.Count == 0) return;

        bool wasClean = !HasUnsavedChanges;
        CenterWindow();
        _firstCentered = true;
        _modelChanged.OnNext(Unit.Default);
        if (wasClean) MarkClean();   // автоцентрирование изменением не считается
    }

    /// <summary>Центрирует раму в холсте. Вызывается при создании и при первом SizeChanged.</summary>
    private void CenterWindow()
    {
        if (_document.Root == null || CanvasWidth <= 0) return;

        var bounds = _document.Root.GetContour().BoundingBox;
        double dx = (CanvasWidth - bounds.Width) / 2.0 - bounds.MinX;
        double dy = (CanvasHeight - bounds.Height) / 2.0 - bounds.MinY;

        if (Math.Abs(dx) > 0.001 || Math.Abs(dy) > 0.001)
            _document.Root.MoveBy(dx, dy);
    }

    /// <summary>
    /// Строит сетку для видимой области холста (с учётом Zoom и Pan).
    /// Линии — в координатах рамы, с шагом GridSize.
    /// </summary>
    private void UpdateGridGeometry()
    {
        var settings = AppSettings.Instance;
        if (!settings.IsGridVisible || settings.GridSize <= 0 || CanvasWidth <= 0 || CanvasHeight <= 0)
        {
            GridGeometry = null;
            return;
        }

        double gridSize = (double)settings.GridSize;
        double zoom = settings.Zoom == 0 ? 1.0 : settings.Zoom;

        // Видимая область в координатах рамы
        double startX = Math.Floor((-settings.PanX / zoom) / gridSize) * gridSize;
        double startY = Math.Floor((-settings.PanY / zoom) / gridSize) * gridSize;
        double endX = Math.Ceiling(((CanvasWidth - settings.PanX) / zoom) / gridSize) * gridSize;
        double endY = Math.Ceiling(((CanvasHeight - settings.PanY) / zoom) / gridSize) * gridSize;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (double x = startX; x <= endX; x += gridSize)
            {
                ctx.BeginFigure(new AvaloniaPoint(x, startY), false);
                ctx.LineTo(new AvaloniaPoint(x, endY));
                ctx.EndFigure(false);
            }
            for (double y = startY; y <= endY; y += gridSize)
            {
                ctx.BeginFigure(new AvaloniaPoint(startX, y), false);
                ctx.LineTo(new AvaloniaPoint(endX, y));
                ctx.EndFigure(false);
            }
        }
        GridGeometry = geometry;
    }
}