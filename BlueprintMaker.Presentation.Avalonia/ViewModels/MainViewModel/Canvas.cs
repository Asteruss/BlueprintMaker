using Avalonia.Media;
using System;
using System.Reactive;
using AvaloniaPoint = global::Avalonia.Point;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class MainViewModel
{
    private bool _firstCentered;
    public event Action? CenterRequested;
    /// <summary>Вид нужно прокрутить к окну. Сбрасывает MainWindow после прокрутки.</summary>
    public bool NeedsViewCenter { get; set; } = true;

    /// <summary>Куда телепортировать вид: центр окна, а если окна нет — центр холста.</summary>
    public (double x, double y) GetViewTarget()
    {
        if (_document.Root != null)
        {
            var b = _document.Root.GetContour().BoundingBox;
            return (b.MinX + b.Width / 2, b.MinY + b.Height / 2);
        }
        return (CanvasWidth / 2, CanvasHeight / 2);
    }

    public (double cx, double cy)? GetWindowCenter()
    {
        if (_document.Root == null) return null;
        var b = _document.Root.GetContour().BoundingBox;
        return (b.MinX + b.Width / 2, b.MinY + b.Height / 2);
    }
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
        NeedsViewCenter = true;
    }

    /// <summary>
    /// Строит сетку для видимой области холста (с учётом Zoom и Pan).
    /// Линии — в координатах рамы, с шагом GridSize.
    /// </summary>
    private void UpdateGridGeometry()
    {
        var settings = AppSettings.Instance;
        if (!settings.IsGridVisible || settings.GridSize <= 0 || CanvasWidth <= 0 || CanvasHeight <= 0)
        { GridGeometry = null; return; }

        double step = (double)settings.GridSize;
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            for (double x = 0; x <= CanvasWidth; x += step)
            { ctx.BeginFigure(new AvaloniaPoint(x, 0), false); ctx.LineTo(new AvaloniaPoint(x, CanvasHeight)); ctx.EndFigure(false); }
            for (double y = 0; y <= CanvasHeight; y += step)
            { ctx.BeginFigure(new AvaloniaPoint(0, y), false); ctx.LineTo(new AvaloniaPoint(CanvasWidth, y)); ctx.EndFigure(false); }
        }
        GridGeometry = geometry;
    }
}