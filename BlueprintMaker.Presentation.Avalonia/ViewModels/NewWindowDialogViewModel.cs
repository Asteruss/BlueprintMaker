using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BlueprintMaker.Core.Geometry;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public enum WindowShapeKind { Rectangle, Trapezoid, Ellipse, Polygon }

/// <summary>Плитка выбора формы. Иконка необязательна: если файла нет, показывается только название.</summary>
public sealed class ShapeOption
{
    public ShapeOption(WindowShapeKind kind, string title, string iconFile)
    {
        Kind = kind;
        Title = title;
        Icon = TryLoad(iconFile);
    }

    public WindowShapeKind Kind { get; }
    public string Title { get; }
    public Bitmap? Icon { get; }

    private static Bitmap? TryLoad(string file)
    {
        try
        {
            // Положите PNG в Assets/Shapes и поставьте Build Action = AvaloniaResource
            var uri = new Uri($"avares://BlueprintMaker.Presentation.Avalonia/Assets/Shapes/{file}");
            return AssetLoader.Exists(uri) ? new Bitmap(AssetLoader.Open(uri)) : null;
        }
        catch { return null; }
    }
}

public sealed record SideOption(ShapeSide Side, string Title);

public partial class NewWindowDialogViewModel : ReactiveObject
{
    public IReadOnlyList<ShapeOption> Shapes { get; } = new[]
    {
        new ShapeOption(WindowShapeKind.Rectangle, "Прямоугольник", "rectangle.png"),
        new ShapeOption(WindowShapeKind.Trapezoid, "Трапеция", "trapezoid.png"),
        new ShapeOption(WindowShapeKind.Ellipse, "Овал / круг", "ellipse.png"),
        new ShapeOption(WindowShapeKind.Polygon, "Многоугольник", "polygon.png"),
    };

    public IReadOnlyList<SideOption> Sides { get; } = new[]
    {
        new SideOption(ShapeSide.Top, "Сверху"),
        new SideOption(ShapeSide.Right, "Справа"),
        new SideOption(ShapeSide.Bottom, "Снизу"),
        new SideOption(ShapeSide.Left, "Слева"),
    };

    [Reactive] private ShapeOption _selectedShape;
    [Reactive] private string? _error;

    // Прямоугольник
    [Reactive] private decimal? _rectWidth = 500;
    [Reactive] private decimal? _rectHeight = 400;

    // Трапеция
    [Reactive] private decimal? _trapBaseWidth = 500;
    [Reactive] private decimal? _trapNarrowWidth = 300;
    [Reactive] private decimal? _trapHeight = 400;
    [Reactive] private SideOption _trapSide;

    // Овал
    [Reactive] private decimal? _radiusX = 250;
    [Reactive] private decimal? _radiusY = 200;

    // Многоугольник
    [Reactive] private decimal? _polySides = 6;
    [Reactive] private decimal? _polySideLength = 200;

    public bool IsRectangle => SelectedShape.Kind == WindowShapeKind.Rectangle;
    public bool IsTrapezoid => SelectedShape.Kind == WindowShapeKind.Trapezoid;
    public bool IsEllipse => SelectedShape.Kind == WindowShapeKind.Ellipse;
    public bool IsPolygon => SelectedShape.Kind == WindowShapeKind.Polygon;

    public NewWindowDialogViewModel()
    {
        _selectedShape = Shapes[0];
        _trapSide = Sides[0];

        this.WhenAnyValue(x => x.SelectedShape).Subscribe(_ =>
        {
            Error = null;
            this.RaisePropertyChanged(nameof(IsRectangle));
            this.RaisePropertyChanged(nameof(IsTrapezoid));
            this.RaisePropertyChanged(nameof(IsEllipse));
            this.RaisePropertyChanged(nameof(IsPolygon));
        });
    }

    /// <summary>Строит контур по введённым параметрам. false — параметры некорректны, причина в Error.</summary>
    public bool TryBuild(out Contour? contour)
    {
        contour = null;
        Error = null;

        try
        {
            switch (SelectedShape.Kind)
            {
                case WindowShapeKind.Rectangle:
                    contour = FrameShapeFactory.Rectangle(D(RectWidth), D(RectHeight));
                    break;

                case WindowShapeKind.Trapezoid:
                    if (D(TrapNarrowWidth) >= D(TrapBaseWidth))
                    {
                        Error = "Узкое основание должно быть меньше широкого.";
                        return false;
                    }
                    contour = FrameShapeFactory.Trapezoid(
                        D(TrapBaseWidth), D(TrapNarrowWidth), D(TrapHeight), TrapSide.Side);
                    break;

                case WindowShapeKind.Ellipse:
                    contour = FrameShapeFactory.Ellipse(D(RadiusX), D(RadiusY));
                    break;

                case WindowShapeKind.Polygon:
                    int sides = (int)D(PolySides);
                    if (sides < 3 || sides > 64)
                    {
                        Error = "Количество сторон: от 3 до 64.";
                        return false;
                    }
                    contour = FrameShapeFactory.RegularPolygon(sides, D(PolySideLength));
                    break;
            }
            return contour != null;
        }
        catch (ArgumentException ex)
        {
            Error = ex.Message;
            return false;
        }
    }

    private static double D(decimal? v) => v.HasValue ? (double)v.Value : 0;
}