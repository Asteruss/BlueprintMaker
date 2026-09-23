using BlueprintMaker.Application;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BlueprintMaker.Presentation.Wpf;

using CorePoint = Core.Geometry.Point;
using WpfPoint = System.Windows.Point;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly Document _document = new();
    private readonly DocumentHistory _history = new();

    private WpfPoint _lastRightClickPoint;
    private PhysicalElement? _draggedElement;
    private int _draggedNodeIndex = -1;
    private Ellipse? _draggedEllipse;

    public MainWindow()
    {
        InitializeComponent();

        // АВТОМАТИЧЕСКОЕ СОЗДАНИЕ РАМЫ
        var vertices = new CorePoint[]
        {
            new CorePoint(50, 50),
            new CorePoint(550, 50),
            new CorePoint(550, 450),
            new CorePoint(50, 450)
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

    // ================= РЕНДЕРИНГ =================

    private void RenderTree()
    {
        DrawingCanvas.Children.Clear();
        if (_document.Root == null) return;
        DrawNodeRecursive(_document.Root);
    }

    private void DrawNodeRecursive(IWindowNode node)
    {
        // 1. Рисуем световые пространства (заливка для кликов ПКМ)
        if (node is LightSpace ls)
        {
            var path = new System.Windows.Shapes.Path
            {
                Data = ls.GetContour().ToWpfGeometry(),
                Fill = ls.Children.Count == 0 ? Brushes.LightGray : Brushes.Transparent,
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

            DrawingCanvas.Children.Add(path);
        }

        // 2. Рисуем физические элементы (Рама, Импост)
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
                    Stroke = node is Domain.Models.Elements.Frame ? Brushes.DodgerBlue :
                         node is Impost ? Brushes.Green : Brushes.Magenta,
                    StrokeThickness = 2,
                    Tag = (phys, i),
                    Cursor = Cursors.Hand
                };

                // Контекстное меню для разбиения сегмента
                var menu = new ContextMenu();
                var splitItem = new MenuItem { Header = "Разбить в точке клика", Tag = (phys, i) };
                splitItem.Click += SplitSegment_Click;
                menu.Items.Add(splitItem);
                path.ContextMenu = menu;

                DrawingCanvas.Children.Add(path);
            }
        }

        // 3. Рисуем узлы (Точки), чтобы их можно было тянуть ЛКМ
        if (node is PhysicalElement physEl && node is not Sash)
        {
            var vertices = physEl.GetContour().Vertices;
            for (int i = 0; i < vertices.Count; i++)
            {
                var p = vertices[i].Position;
                var ellipse = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Yellow,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1,
                    Tag = (physEl, i),
                    Cursor = Cursors.SizeAll
                };
                ellipse.MouseLeftButtonDown += Node_MouseLeftButtonDown;
                Canvas.SetLeft(ellipse, p.X - 5);
                Canvas.SetTop(ellipse, p.Y - 5);
                DrawingCanvas.Children.Add(ellipse);
            }
        }

        foreach (var child in node.Children)
        {
            DrawNodeRecursive(child);
        }
    }

    // ================= ОБРАБОТЧИКИ =================

    private void InsertImpost(LightSpace space, AxisOrientation orient)
    {
        _history.Execute(new InsertImpostCommand(space, orient, 0.5));
        RenderTree();
    }

    private void InsertSash(LightSpace space)
    {
        _history.Execute(new InsertSashCommand(space));
        RenderTree();
    }

    private void DrawingCanvas_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        _lastRightClickPoint = e.GetPosition(DrawingCanvas);
    }

    private void SplitSegment_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is ValueTuple<PhysicalElement, int> tag)
        {
            var pos = new CorePoint(_lastRightClickPoint.X, _lastRightClickPoint.Y);
            _history.Execute(new SplitSegmentCommand(tag.Item1, tag.Item2, pos));
            RenderTree();
        }
    }

    // ТЯГА УЗЛОВ
    private void Node_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Ellipse ellipse && ellipse.Tag is ValueTuple<PhysicalElement, int> tag)
        {
            _draggedElement = tag.Item1;
            _draggedNodeIndex = tag.Item2;
            _draggedEllipse = ellipse;
            _draggedEllipse.CaptureMouse();

            // Подписываемся на события мыши на уровне окна
            this.MouseMove += MainWindow_MouseMove;
            this.MouseLeftButtonUp += MainWindow_MouseLeftButtonUp;
            e.Handled = true;
        }
    }

    private void MainWindow_MouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedEllipse != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            Canvas.SetLeft(_draggedEllipse, pos.X - 5);
            Canvas.SetTop(_draggedEllipse, pos.Y - 5);
        }
    }

    private void MainWindow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedElement != null && _draggedEllipse != null)
        {
            var pos = e.GetPosition(DrawingCanvas);
            var corePos = new CorePoint(pos.X, pos.Y);
            _history.Execute(new MoveNodeCommand(_draggedElement, _draggedNodeIndex, corePos));

            this.MouseMove -= MainWindow_MouseMove;
            this.MouseLeftButtonUp -= MainWindow_MouseLeftButtonUp;
            _draggedEllipse.ReleaseMouseCapture();
            _draggedEllipse = null;
            _draggedElement = null;

            RenderTree(); // Перерисовываем с актуальной геометрией
        }
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        _history.Undo();
        RenderTree();
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        _history.Redo();
        RenderTree();
    }
}