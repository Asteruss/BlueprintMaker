using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using BlueprintMaker.Core.Geometry;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;

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

    // Овал / круг: два радиуса (горизонтальный и вертикальный)
    public IReadOnlyList<int> BoardCounts => FrameShapeFactory.OvalBoardCounts;
    [Reactive] private int _ovalBoards = 4;
    [Reactive] private decimal? _radiusHorizontal = 250;
    [Reactive] private decimal? _radiusVertical = 200;

    // Многоугольник
    [Reactive] private decimal? _polySides = 6;
    [Reactive] private decimal? _polySideLength = 200;

    public bool IsRectangle => SelectedShape.Kind == WindowShapeKind.Rectangle;
    public bool IsTrapezoid => SelectedShape.Kind == WindowShapeKind.Trapezoid;
    public bool IsEllipse => SelectedShape.Kind == WindowShapeKind.Ellipse;
    public bool IsPolygon => SelectedShape.Kind == WindowShapeKind.Polygon;

    private bool IsCircle => Math.Abs(D(RadiusHorizontal) - D(RadiusVertical)) < 1e-6;

    public string OvalInfo =>
        $"Размер: {2 * D(RadiusHorizontal):0.##} × {2 * D(RadiusVertical):0.##} мм" +
        (IsCircle ? " (круг)" : "");

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

        this.WhenAnyValue(x => x.RadiusHorizontal, x => x.RadiusVertical, x => x.OvalBoards)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(OvalInfo)));
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
                    if (!IsCircle && OvalBoards < 4)
                    {
                        Error = "При 2 и 3 досках возможен только круг. Выберите 4 или 6 досок.";
                        return false;
                    }
                    contour = FrameShapeFactory.Oval(D(RadiusHorizontal), D(RadiusVertical), OvalBoards);
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