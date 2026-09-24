using BlueprintMaker.Application;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BlueprintMaker.Presentation.Wpf;

using CorePoint = Core.Geometry.Point;
using WpfPoint = System.Windows.Point;

public partial class MainWindow : Window
{
    private readonly Document _document = new();
    private readonly DocumentHistory _history = new();

    private WpfPoint _lastRightClickPoint;

    // Состояния перетаскивания
    private PhysicalElement? _draggedElement;
    private int _draggedNodeIndex = -1;
    private Ellipse? _draggedEllipse;

    private PhysicalElement? _draggedArcElement;
    private int _draggedArcSegmentIndex = -1;
    private Ellipse? _draggedArcEllipse;

    public MainWindow()
    {
        InitializeComponent();
        CreateInitialFrame(500, 400);
    }

    private void CreateInitialFrame(double width, double height)
    {
        _document.Reset();
        _history.Clear();

        var vertices = new CorePoint[]
        {
            new CorePoint(50, 50),
            new CorePoint(width + 50, 50),
            new CorePoint(width + 50, height + 50),
            new CorePoint(50, height + 50)
        };

        var contour = Contour.CreateClosed(new[]
        {
            new ContourVertex(vertices[0]),
            new ContourVertex(vertices[1]),
            new ContourVertex(vertices[2]),
            new ContourVertex(vertices[3])
        });

        _history.Execute(new CreateFrameCommand(_document, contour));
        RenderTree();
    }

    private void CreateCustomFrame_Click(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(WidthBox.Text, out double w) && double.TryParse(HeightBox.Text, out double h))
        {
            CreateInitialFrame(w, h);
        }
    }

    // ================= РЕНДЕРИНГ =================

    private void RenderTree()
    {
        DrawingCanvas.Children.Clear();
        if (_document.Root == null) return;

        // Разделяем отрисовку по Z-index (слоям), чтобы заливка не закрывала линии
        var fillLayer = new List<UIElement>();
        var strokeLayer = new List<UIElement>();
        var nodeLayer = new List<UIElement>();
        var controlLayer = new List<UIElement>();

        DrawNodeRecursive(_document.Root, fillLayer, strokeLayer, nodeLayer, controlLayer);

        foreach (var e in fillLayer) DrawingCanvas.Children.Add(e);
        foreach (var e in strokeLayer) DrawingCanvas.Children.Add(e);
        foreach (var e in nodeLayer) DrawingCanvas.Children.Add(e);
        foreach (var e in controlLayer) DrawingCanvas.Children.Add(e);
    }

    private void DrawNodeRecursive(IWindowNode node, List<UIElement> fill, List<UIElement> strokes, List<UIElement> nodes, List<UIElement> controls)
    {
        // 1. Заливка световых пространств (Белая, чтобы не перекрывать линии)
        if (node is LightSpace ls)
        {
            var path = new System.Windows.Shapes.Path
            {
                Data = ls.GetContour().ToWpfGeometry(),
                Fill = ls.Children.Count == 0 ? Brushes.White : Brushes.Transparent,
                Stroke = Brushes.Transparent,
                StrokeThickness = 0,
                Tag = ls
            };

            var menu = new ContextMenu();
            var itemImpostV = new MenuItem { Header = "Вставить вертикальный импост" };
            itemImpostV.Click += (s, e) => InsertImpost(ls, AxisOrientation.Vertical);
            var itemImpostH = new MenuItem { Header = "Вставить горизонтальный импост" };
            itemImpostH.Click += (s, e) => InsertImpost(ls, AxisOrientation.Horizontal);
            var itemSash = new MenuItem { Header = "Вставить створку" };
            itemSash.Click += (s, e) => InsertSash(ls);

            menu.Items.Add(itemImpostV);
            menu.Items.Add(itemImpostH);
            menu.Items.Add(itemSash);
            path.ContextMenu = menu;

            fill.Add(path);
        }

        // 2. Физические элементы (Рамы, Импосты, Доборы - ЛЮБЫЕ ПАЛКИ)
        if (node is PhysicalElement phys)
        {
            var contour = phys.GetContour();
            int segmentsCount = contour.Vertices.Count - (contour.IsClosed ? 0 : 1);
            for (int i = 0; i < segmentsCount; i++)
            {
                var from = contour.Vertices[i];
                var to = contour.Vertices[(i + 1) % contour.Vertices.Count];

                var segmentContour = Contour.CreateOpen(new[] { from, to });
                var path = new System.Windows.Shapes.Path
                {
                    Data = segmentContour.ToWpfGeometry(),
                    Stroke = 
             node is Sash ? Brushes.Orange :
             node is Domain.Models.Elements.Frame ? Brushes.DodgerBlue :
             node is Impost ? Brushes.Lime : Brushes.Magenta,
                    StrokeThickness = phys.GetPhysicalWidth(),
                    Tag = (phys, i),
                    Cursor = Cursors.Hand
                };

                var menu = new ContextMenu();
                var splitItem = new MenuItem { Header = "Разбить в точке клика", Tag = (phys, i) };
                splitItem.Click += SplitSegment_Click;
                var jambExtItem = new MenuItem { Header = "Добавить внешний доборник", Tag = (phys, i) };
                jambExtItem.Click += AddJambExt_Click;
                var jambIntItem = new MenuItem { Header = "Добавить внутренний доборник", Tag = (phys, i) };
                jambIntItem.Click += AddJambInt_Click;
                var arcItem = new MenuItem { Header = "Сделать дугой (вниз)", Tag = (phys, i) };
                arcItem.Click += MakeArc_Click;
                var straightItem = new MenuItem { Header = "Выпрямить линию", Tag = (phys, i) };
                straightItem.Click += MakeStraight_Click;

                menu.Items.Add(splitItem);
                menu.Items.Add(jambExtItem);
                menu.Items.Add(jambIntItem);
                menu.Items.Add(arcItem);
                menu.Items.Add(straightItem);
                path.ContextMenu = menu;

                strokes.Add(path);

                if (Math.Abs(from.Bulge) > 0.0001)
                {
                    double chord = from.Position.DistanceTo(to.Position);
                    double h = Math.Abs(from.Bulge) * chord / 2.0; // Высота дуги (всегда положительная)

                    var midBase = new CorePoint((from.Position.X + to.Position.X) / 2, (from.Position.Y + to.Position.Y) / 2);

                    double dx = to.Position.X - from.Position.X;
                    double dy = to.Position.Y - from.Position.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);

                    // Правая нормаль вектора (для экрана это наружу при обходе по часовой)
                    double nx = -dy / len;
                    double ny = dx / len;

                    // Если Bulge отрицательный (дуга в другую сторону), инвертируем нормаль
                    if (from.Bulge < 0)
                    {
                        nx = -nx;
                        ny = -ny;
                    }

                    var midPoint = new CorePoint(midBase.X + nx * h, midBase.Y + ny * h);

                    var controlEllipse = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = Brushes.Red,
                        Stroke = Brushes.White,
                        StrokeThickness = 2,
                        Tag = (phys, i),
                        Cursor = Cursors.SizeAll
                    };
                    controlEllipse.MouseLeftButtonDown += ArcControl_MouseLeftButtonDown;
                    Canvas.SetLeft(controlEllipse, midPoint.X - 5);
                    Canvas.SetTop(controlEllipse, midPoint.Y - 5);
                    controls.Add(controlEllipse);
                }
            }
        }

        // 3. Узлы (Точки)
        if (node is PhysicalElement physEl && node is not Sash)
        {
            var vertices = physEl.GetContour().Vertices;
            for (int i = 0; i < vertices.Count; i++)
            {
                var p = vertices[i].Position;
                var ellipse = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = Brushes.Yellow,
                    Stroke = Brushes.Black,
                    StrokeThickness = physEl.GetPhysicalWidth(),
                    Tag = (physEl, i),
                    Cursor = Cursors.SizeAll
                };
                ellipse.MouseLeftButtonDown += Node_MouseLeftButtonDown;
                Canvas.SetLeft(ellipse, p.X - 6);
                Canvas.SetTop(ellipse, p.Y - 6);
                nodes.Add(ellipse);
            }
        }

        foreach (var child in node.Children) DrawNodeRecursive(child, fill, strokes, nodes, controls);
    }

    // ================= ОБРАБОТЧИКИ =================

    private void InsertImpost(LightSpace space, AxisOrientation orient) { _history.Execute(new InsertImpostCommand(space, orient, 0.5)); RenderTree(); }
    private void InsertSash(LightSpace space) { _history.Execute(new InsertSashCommand(space)); RenderTree(); }
    private void DrawingCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) { _lastRightClickPoint = e.GetPosition(DrawingCanvas); }
    private void SplitSegment_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem m && m.Tag is ValueTuple<PhysicalElement, int> t) { _history.Execute(new SplitSegmentCommand(t.Item1, t.Item2, new CorePoint(_lastRightClickPoint.X, _lastRightClickPoint.Y))); RenderTree(); } }
    private void AddJambExt_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem m && m.Tag is ValueTuple<PhysicalElement, int> t) { _history.Execute(new AddJambCommand(t.Item1, t.Item2, null)); RenderTree(); } }
    private void AddJambInt_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem m && m.Tag is ValueTuple<PhysicalElement, int> t)
        {
            LightSpace? target = t.Item1 is Domain.Models.Elements.Frame f ? f.InnerSpace : t.Item1 is Impost im ? im.Children.OfType<LightSpace>().FirstOrDefault() : null;
            if (target != null) { _history.Execute(new AddJambCommand(t.Item1, t.Item2, target)); RenderTree(); }
        }
    }
    private void MakeArc_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem m && m.Tag is ValueTuple<PhysicalElement, int> t) { _history.Execute(new SetSegmentArcCommand(t.Item1, t.Item2, 0.5)); RenderTree(); } }
    private void MakeStraight_Click(object sender, RoutedEventArgs e) { if (sender is MenuItem m && m.Tag is ValueTuple<PhysicalElement, int> t) { _history.Execute(new SetSegmentArcCommand(t.Item1, t.Item2, 0.0)); RenderTree(); } }

    // ТЯГА УЗЛОВ
    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse el && el.Tag is ValueTuple<PhysicalElement, int> tag)
        {
            _draggedElement = tag.Item1; _draggedNodeIndex = tag.Item2; _draggedEllipse = el;
            _draggedEllipse.CaptureMouse();
            this.MouseMove += MainWindow_MouseMove; this.MouseLeftButtonUp += MainWindow_MouseLeftButtonUp;
            e.Handled = true;
        }
    }

    private void MainWindow_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedEllipse != null) { var p = e.GetPosition(DrawingCanvas); Canvas.SetLeft(_draggedEllipse, p.X - 6); Canvas.SetTop(_draggedEllipse, p.Y - 6); }
    }

    private void MainWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedElement != null && _draggedEllipse != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var corePos = new CorePoint(pos.X, pos.Y);

            // ПРИКЛЕИВАНИЕ (Snapping) к раме для Импостов
            if (_draggedElement is Impost && _document.Root != null)
            {
                var boundary = _document.Root.InnerSpace.GetContour();
                double minDist = 15.0; // Допуск 15 пикселей
                for (int i = 0; i < boundary.Vertices.Count; i++)
                {
                    var from = boundary.Vertices[i]; var to = boundary.Vertices[(i + 1) % boundary.Vertices.Count];
                    var proj = SegmentProjection.Project(from.Position, to.Position, from.Bulge, corePos);
                    if (proj.Distance < minDist) { minDist = proj.Distance; corePos = proj.ClosestPoint; }
                }
            }

            _history.Execute(new MoveNodeCommand(_draggedElement, _draggedNodeIndex, corePos));
            this.MouseMove -= MainWindow_MouseMove; this.MouseLeftButtonUp -= MainWindow_MouseLeftButtonUp;
            _draggedEllipse.ReleaseMouseCapture(); _draggedEllipse = null; _draggedElement = null;
            RenderTree();
        }
    }

    // ТЯГА ДУГ (Control Points)
    private void ArcControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse el && el.Tag is ValueTuple<PhysicalElement, int> tag)
        {
            _draggedArcElement = tag.Item1; _draggedArcSegmentIndex = tag.Item2; _draggedArcEllipse = el;
            _draggedArcEllipse.CaptureMouse();
            this.MouseMove += ArcControl_MouseMove; this.MouseLeftButtonUp += ArcControl_MouseLeftButtonUp;
            e.Handled = true;
        }
    }

    private void ArcControl_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedArcEllipse != null) { var p = e.GetPosition(DrawingCanvas); Canvas.SetLeft(_draggedArcEllipse, p.X - 5); Canvas.SetTop(_draggedArcEllipse, p.Y - 5); }
    }

    private void ArcControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedArcElement != null && _draggedArcEllipse != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var corePos = new CorePoint(pos.X, pos.Y);
            _history.Execute(new MoveArcControlPointCommand(_draggedArcElement, _draggedArcSegmentIndex, corePos));
            this.MouseMove -= ArcControl_MouseMove; this.MouseLeftButtonUp -= ArcControl_MouseLeftButtonUp;
            _draggedArcEllipse.ReleaseMouseCapture(); _draggedArcEllipse = null; _draggedArcElement = null;
            RenderTree();
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e) { _history.Undo(); RenderTree(); }
    private void Redo_Click(object sender, RoutedEventArgs e) { _history.Redo(); RenderTree(); }
}