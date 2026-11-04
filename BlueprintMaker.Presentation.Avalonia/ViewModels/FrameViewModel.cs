using Avalonia.Media;
using BlueprintMaker.Application;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Presentation.Avalonia.Helpers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Security.Cryptography;
using AvaloniaPoint = Avalonia.Point;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class FrameViewModel : SelectableElement
{
    public readonly Frame Frame;
    private readonly DocumentHistory _history;
    private readonly ReactiveCommand<SelectableElement, Unit> _selectCmd;
    private readonly ReactiveCommand<JambSlot, Unit> _addJambCmd;

    // Коллекции для холста
    public ObservableCollection<WireframeLine> Lines { get; } = new();
    public ObservableCollection<NodeViewModel> Nodes { get; } = new();
    public ObservableCollection<BoardViewModel> Boards { get; } = new();
    public ObservableCollection<LightSpaceViewModel> Spaces { get; } = new();
    public ObservableCollection<ImpostViewModel> Imposts { get; } = new();
    public ObservableCollection<SashViewModel> Sashes { get; } = new();
    public ObservableCollection<JambViewModel> Jambs { get; } = new();
    public ObservableCollection<JambSlotViewModel> JambSlots { get; } = new();
    public ObservableCollection<DimensionViewModel> Dimensions { get; } = new();
    [Reactive] private FrameDragHandle _frameDragHandle;
    [Reactive] private bool _isMaterialized;

    /// <summary>Режим показа стрелок создания добора.</summary>
    [Reactive] private bool _showJambSlots = true;
    private IWindowNode? _jambTarget;
    public FrameAssembly Assembly => Frame.Assembly;
    public double Zoom => AppSettings.Instance.Zoom;

    public FrameViewModel(
        Frame frame,
        DocumentHistory history,
        ReactiveCommand<SelectableElement, Unit> selectCmd,
        ReactiveCommand<JambSlot, Unit> addJambCmd) : base(selectCmd)
    {
        Frame = frame;
        _history = history;
        _selectCmd = selectCmd;
        _addJambCmd = addJambCmd;
        DisplayName = "Рама";

        AppSettings.Instance.WhenAnyValue(x => x.Zoom).Subscribe(_ => this.RaisePropertyChanged(nameof(Zoom)));
        this.WhenAnyValue(x => x.ShowJambSlots).Subscribe(_ => RefreshJambSlots());

        Refresh();
    }

    public void Refresh()
    {
        Lines.Clear();
        Nodes.Clear();
        Boards.Clear();
        Spaces.Clear();
        Sashes.Clear();
        Imposts.Clear();
        Jambs.Clear();
        Children.Clear();
        FrameDragHandle = null;
        Dimensions.Clear();

        BuildTree(Frame, this);
        RefreshJambSlots();
        RefreshTreeChildrenRecursive();
    }

    public void SetJambTarget(IWindowNode? node)
    {
        _jambTarget = node;
        RefreshJambSlots();
    }

    private void RefreshJambSlots()
    {
        JambSlots.Clear();
        if (_jambTarget == null) return;

        foreach (var slot in JambSlotBuilder.Build(_jambTarget))
            JambSlots.Add(new JambSlotViewModel(slot, _addJambCmd));
    }

    private void BuildTree(IWindowNode node, SelectableElement parentVM)
    {
        if (node is Sash sash)
        {
            var sashVM = new SashViewModel(sash, _selectCmd);
            parentVM.Children.Add(sashVM);
            Sashes.Add(sashVM);

            sashVM.RefreshBoards();
            sashVM.RefreshTreeChildren();

            parentVM = sashVM;
        }
        else if (node is Frame frame)
        {
            var contour = frame.GetContour();
            var bounds = contour.BoundingBox;
            var center = new Core.Geometry.Point(
                (bounds.MinX + bounds.MaxX) / 2.0,
                (bounds.MinY + bounds.MaxY) / 2.0);

            // Линии рамы
            for (int i = 0; i < contour.Vertices.Count; i++)
            {
                var from = contour.Vertices[i];
                var to = contour.Vertices[(i + 1) % contour.Vertices.Count];
                var line = new WireframeLine(frame, i, from, to, _selectCmd);
                parentVM.Children.Add(line);
                Lines.Add(line);

                var ctrl = ComputeArcControlPoint(from.Position, to.Position, from.Bulge);
                if (ctrl.HasValue && frame.Width == 0)
                {
                    var arcNode = new NodeViewModel(frame, i, ctrl.Value, _selectCmd, NodeType.ArcControl);
                    parentVM.Children.Add(arcNode);
                    Nodes.Add(arcNode);
                }
            }

            if (frame.Width > 0)
            {
                IsMaterialized = true;
                int boardIndex = 0;
                foreach (var board in frame.Assembly.Boards)
                {
                    var bvm = new BoardViewModel(board, _selectCmd);

                    // Сторона света
                    var mid = new Core.Geometry.Point(
                        (board.StartOuter.X + board.EndOuter.X) / 2.0,
                        (board.StartOuter.Y + board.EndOuter.Y) / 2.0);
                    var label = SideLabelResolver.GetLabel(mid, center);
                    bvm.DisplayName = $"Рама {label} {bvm.BoardLength:F0}мм";

                    parentVM.Children.Add(bvm);
                    Boards.Add(bvm);

                    var p = board.StartOuter;
                    var nvm = new NodeViewModel(frame, boardIndex, p, _selectCmd, NodeType.FrameCorner);
                    parentVM.Children.Add(nvm);
                    Nodes.Add(nvm);

                    boardIndex++;
                }
            }
            else
            {
                IsMaterialized = false;
                for (int i = 0; i < contour.Vertices.Count; i++)
                {
                    var p = contour.Vertices[i].Position;
                    var nvm = new NodeViewModel(frame, i, p, _selectCmd, NodeType.FrameCorner);
                    parentVM.Children.Add(nvm);
                    Nodes.Add(nvm);
                }
            }

            FrameDragHandle = new FrameDragHandle(frame, _selectCmd);
            // ВНИМАНИЕ: CreateOverallDimensions / CreateChainDimensions перенесены
            // ПОСЛЕ рекурсии по детям — иначе коллекция Imposts ещё пуста.
        }
        else if (node is LightSpace ls)
        {
            var lsVM = new LightSpaceViewModel(ls, _selectCmd);
            parentVM.Children.Add(lsVM);
            Spaces.Add(lsVM);
            parentVM = lsVM;
        }
        else if (node is Impost impost)
        {
            impost.ReprojectToParent();
            var impVM = new ImpostViewModel(impost, _selectCmd);

            var impContour = impost.GetContour();

            // Метка ориентации берётся НАПРЯМУЮ из Impost.Orientation — это
            // начальное значение, заданное при создании импоста. Оно НЕ меняется
            // при drag'е node (Rule замораживается, но Orientation сохраняется).
            // Поэтому метка всегда корректна: «В» для вертикального, «Г» для горизонтального.
            var impLabel = impost.Orientation == AxisOrientation.Horizontal ? "Г" : "В";

            // Длина
            double impLength = GetImpostLength(impost);

            impVM.DisplayName = $"Импост {impLabel} {impLength:F0}мм";

            parentVM.Children.Add(impVM);
            Imposts.Add(impVM);

            for (int i = 0; i < impContour.Vertices.Count; i++)
            {
                var p = impContour.Vertices[i].Position;
                var nodeType = (i == 0 || i == impContour.Vertices.Count - 1) ? NodeType.ImpostEnd : NodeType.ImpostSplit;
                var nvm = new NodeViewModel(impost, i, p, _selectCmd, nodeType);
                parentVM.Children.Add(nvm);
                if (impost.Width == 0)
                    Nodes.Add(nvm);
            }

            for (int i = 0; i < impContour.Vertices.Count - 1; i++)
            {
                var from = impContour.Vertices[i];
                var to = impContour.Vertices[i + 1];
                var ctrl = ComputeArcControlPoint(from.Position, to.Position, from.Bulge);
                if (ctrl.HasValue && impost.Width == 0)
                {
                    var arcNode = new NodeViewModel(impost, i, ctrl.Value, _selectCmd, NodeType.ArcControl);
                    parentVM.Children.Add(arcNode);
                    Nodes.Add(arcNode);
                }
            }

            parentVM = impVM;
        }
        else if (node is Jamb jamb)
        {
            var jvm = new JambViewModel(jamb, _selectCmd);
            var jambBounds = jamb.GetContour().BoundingBox;
            double jambLength = Math.Max(jambBounds.Width, jambBounds.Height);
            var jambSide = jamb.Side == JambSide.Inner ? "Внутр." : "Внеш.";
            jvm.DisplayName = $"Добор {jambSide} {jambLength:F0}мм";
            parentVM.Children.Add(jvm);
            Jambs.Add(jvm);
            return;
        }

        foreach (var child in node.Children)
            BuildTree(child, parentVM);

        // === Размеры создаются ПОСЛЕ того, как всё дерево построено ===
        // Только тогда коллекция Imposts уже заполнена.
        // ВНИМАНИЕ: Sash наследуется от Frame, поэтому "node is Frame" сработало бы
        // и для створки — это приводило к багу: габариты створки рисовались как
        // для новой рамы. Явно исключаем Sash.
        if (node is Frame fr && node is not Sash)
        {
            CreateOverallDimensions(fr);
            CreateChainDimensions(fr);
        }
    }

    private void CreateOverallDimensions(Frame frame)
    {
        var bounds = frame.GetContour().BoundingBox;

        foreach (var child in frame.Children)
        {
            if (child is Jamb jamb && jamb.Side == JambSide.Outer && jamb.Width > 0)
            {
                var jb = jamb.GetContour().BoundingBox;
                bounds = bounds.Union(new Core.Geometry.Point(jb.MinX, jb.MinY));
                bounds = bounds.Union(new Core.Geometry.Point(jb.MaxX, jb.MaxY));
            }
        }

        // offset = 55: общий габарит теперь ВНЕШНИЙ (дальше от рамы),
        // цепочка внутренних размеров — ближе (offset = 30).
        const double offset = 55;
        const double tickLen = 2.5;

        // === СНИЗУ — ширина ===
        double bottomY = bounds.MaxY + offset;
        var bGeom = new StreamGeometry();
        using (var ctx = bGeom.Open())
        {
            ctx.BeginFigure(new AvaloniaPoint(bounds.MinX, bottomY), false);
            ctx.LineTo(new AvaloniaPoint(bounds.MaxX, bottomY));
            ctx.EndFigure(false);
            ctx.BeginFigure(new AvaloniaPoint(bounds.MinX, bottomY - tickLen), false);
            ctx.LineTo(new AvaloniaPoint(bounds.MinX, bottomY + tickLen));
            ctx.EndFigure(false);
            ctx.BeginFigure(new AvaloniaPoint(bounds.MaxX, bottomY - tickLen), false);
            ctx.LineTo(new AvaloniaPoint(bounds.MaxX, bottomY + tickLen));
            ctx.EndFigure(false);
        }
        var bDim = new DimensionViewModel(bGeom, $"{bounds.Width:F0}",
            (bounds.MinX + bounds.MaxX) / 2.0, bottomY - 8, 0, _selectCmd);
        Children.Add(bDim);
        Dimensions.Add(bDim);

        // === СЛЕВА — высота ===
        double leftX = bounds.MinX - offset;
        var lGeom = new StreamGeometry();
        using (var ctx = lGeom.Open())
        {
            ctx.BeginFigure(new AvaloniaPoint(leftX, bounds.MinY), false);
            ctx.LineTo(new AvaloniaPoint(leftX, bounds.MaxY));
            ctx.EndFigure(false);
            ctx.BeginFigure(new AvaloniaPoint(leftX - tickLen, bounds.MinY), false);
            ctx.LineTo(new AvaloniaPoint(leftX + tickLen, bounds.MinY));
            ctx.EndFigure(false);
            ctx.BeginFigure(new AvaloniaPoint(leftX - tickLen, bounds.MaxY), false);
            ctx.LineTo(new AvaloniaPoint(leftX + tickLen, bounds.MaxY));
            ctx.EndFigure(false);
        }
        var lDim = new DimensionViewModel(lGeom, $"{bounds.Height:F0}",
            leftX - 8, (bounds.MinY + bounds.MaxY) / 2.0, -90, _selectCmd);
        Children.Add(lDim);
        Dimensions.Add(lDim);
    }

    /// <summary>
    /// Цепочки размеров строго на ДВУХ перпендикулярных линиях:
    /// горизонтальная снизу и вертикальная слева. Никаких наклонных
    /// размерных линий.
    ///
    /// Источники координат — ТОЛЬКО осевые линии, независимо от материализации:
    ///   - рама: каждая вершина внешнего контура; для дугового сегмента
    ///     дополнительно — вершина дуги (apex, наиболее удалённая от хорды точка)
    ///   - импост: только КРАЙНИЕ точки central line (первая и последняя вершина);
    ///     точки ImpostSplit (в середине) НЕ учитываются; для дугового сегмента
    ///     central line — вершина дуги
    ///   - створки и доборы игнорируются
    ///
    /// Если осевая линия перпендикулярна (оба конца на одной вертикали/горизонтали),
    /// Distinct() убирает дубликат — в цепочке остаётся ОДНА отметка.
    /// Если наклонная — в цепочке остаются ДВЕ разные координаты, между ними
    /// образуется отрезок с размером = проекция импоста на эту ось.
    /// Ориентацию импоста НЕ используем — только фактические координаты концов.
    /// </summary>
    private void CreateChainDimensions(Frame frame)
    {
        var bounds = frame.GetContour().BoundingBox;

        // offset = 30: внутренние размеры (цепочка) ближе к раме,
        // общий габарит — снаружи (offset = 55).
        const double offset = 30;
        const double tickLen = 2.5;

        // === Сбор всех ключевых координат ===
        // X — для нижней (горизонтальной) цепочки,
        // Y — для левой (вертикальной) цепочки.
        var xStops = new List<double>();
        var yStops = new List<double>();

        // 1) Рама: все вершины внешнего контура + apex каждого дугового сегмента
        var frameContour = frame.GetContour();
        int nFrame = frameContour.Vertices.Count;
        bool frameClosed = frameContour.IsClosed;
        for (int i = 0; i < nFrame; i++)
        {
            var v = frameContour.Vertices[i];
            xStops.Add(v.Position.X);
            yStops.Add(v.Position.Y);

            // Дуговой сегмент: v → следующий (замыкаем, если контур закрыт)
            int nextIdx = (i + 1) % nFrame;
            if (!frameClosed && nextIdx == 0) break;

            var nextV = frameContour.Vertices[nextIdx];
            if (Math.Abs(v.Bulge) > GeometryConstants.Tolerance)
            {
                var apex = ComputeArcApex(v.Position, nextV.Position, v.Bulge);
                if (apex.HasValue)
                {
                    xStops.Add(apex.Value.X);
                    yStops.Add(apex.Value.Y);
                }
            }
        }

        // 2) Импосты: только КРАЙНИЕ точки central line (start, end),
        //    без ImpostSplit. Если central line дуговая — добавим apex дуги.
        foreach (var impVM in Imposts)
        {
            var impost = impVM.SourceImpost;
            var contour = impost.GetContour();
            if (contour.Vertices.Count < 2) continue;

            var start = contour.Vertices[0].Position;
            var end = contour.Vertices[contour.Vertices.Count - 1].Position;

            // Оба конца в обе цепочки. Если совпадают по одной оси (tol),
            // Distinct() их сольёт → останется одна отметка.
            xStops.Add(start.X);
            xStops.Add(end.X);
            yStops.Add(start.Y);
            yStops.Add(end.Y);

            // Apex дуговых сегментов central line (для надёжности обходим все,
            // хотя обычно central line импоста — один отрезок)
            for (int i = 0; i < contour.Vertices.Count - 1; i++)
            {
                var v = contour.Vertices[i];
                var nextV = contour.Vertices[i + 1];
                if (Math.Abs(v.Bulge) > GeometryConstants.Tolerance)
                {
                    var apex = ComputeArcApex(v.Position, nextV.Position, v.Bulge);
                    if (apex.HasValue)
                    {
                        xStops.Add(apex.Value.X);
                        yStops.Add(apex.Value.Y);
                    }
                }
            }
        }

        xStops.Sort();
        xStops = xStops.Distinct().ToList();
        yStops.Sort();
        yStops = yStops.Distinct().ToList();

        // Рисуем цепочку только если есть хотя бы один разделитель внутри —
        // иначе это просто дубликат общих габаритов.
        if (xStops.Count > 2)
        {
            double chainY = bounds.MaxY + offset;
            for (int i = 0; i < xStops.Count - 1; i++)
            {
                double ax = xStops[i];
                double bx = xStops[i + 1];
                double segLen = bx - ax;

                // Пропускаем нулевые / около-нулевые сегменты — они возникают,
                // когда координаты близки, но не точно равны (floating point),
                // и Distinct() их не сливает. Без этого рисовался бы текст "0".
                if (segLen < 0.5) continue;

                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new AvaloniaPoint(ax, chainY), false);
                    ctx.LineTo(new AvaloniaPoint(bx, chainY));
                    ctx.EndFigure(false);
                    ctx.BeginFigure(new AvaloniaPoint(ax, chainY - tickLen), false);
                    ctx.LineTo(new AvaloniaPoint(ax, chainY + tickLen));
                    ctx.EndFigure(false);
                    ctx.BeginFigure(new AvaloniaPoint(bx, chainY - tickLen), false);
                    ctx.LineTo(new AvaloniaPoint(bx, chainY + tickLen));
                    ctx.EndFigure(false);
                }

                double textX = (ax + bx) / 2.0;
                var dim = new DimensionViewModel(geom, $"{segLen:F0}", textX, chainY - 8, 0, _selectCmd);
                Children.Add(dim);
                Dimensions.Add(dim);
            }
        }

        if (yStops.Count > 2)
        {
            double chainX = bounds.MinX - offset;
            for (int i = 0; i < yStops.Count - 1; i++)
            {
                double ay = yStops[i];
                double by = yStops[i + 1];
                double segLen = by - ay;

                // Аналогично — пропускаем нулевые сегменты, иначе текст "0".
                if (segLen < 0.5) continue;

                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new AvaloniaPoint(chainX, ay), false);
                    ctx.LineTo(new AvaloniaPoint(chainX, by));
                    ctx.EndFigure(false);
                    ctx.BeginFigure(new AvaloniaPoint(chainX - tickLen, ay), false);
                    ctx.LineTo(new AvaloniaPoint(chainX + tickLen, ay));
                    ctx.EndFigure(false);
                    ctx.BeginFigure(new AvaloniaPoint(chainX - tickLen, by), false);
                    ctx.LineTo(new AvaloniaPoint(chainX + tickLen, by));
                    ctx.EndFigure(false);
                }

                double textY = (ay + by) / 2.0;
                var dim = new DimensionViewModel(geom, $"{segLen:F0}", chainX - 8, textY, -90, _selectCmd);
                Children.Add(dim);
                Dimensions.Add(dim);
            }
        }
    }

    /// <summary>
    /// Вершина дуги (apex) — точка на дуге, наиболее удалённая от хорды.
    /// Используется для цепочек размеров: дуговой сегмент добавляет в
    /// xStops/yStops координаты своей вершины, чтобы размерные линии
    /// охватывали экстремумы дуги, а не только её концы.
    /// Возвращает null для прямого сегмента (Bulge == 0) или нулевой хорды.
    /// </summary>
    private static Point? ComputeArcApex(Point from, Point to, double bulge)
    {
        if (Math.Abs(bulge) < GeometryConstants.Tolerance) return null;

        var mid = new Point((from.X + to.X) / 2.0, (from.Y + to.Y) / 2.0);

        var ab = new Point(to.X - from.X, to.Y - from.Y);
        double abLen = Math.Sqrt(ab.X * ab.X + ab.Y * ab.Y);
        if (abLen < 1e-9) return null;

        // Перпендикуляр к хорде (единичный)
        var perp = new Point(ab.Y / abLen, -ab.X / abLen);
        // Sagitta = abLen * bulge / 2 (стандартная формула для Bulge = tan(α/4))
        double s = abLen * bulge / 2.0;

        // Знак bulge определяет сторону: >0 — дуга против часовой, perp уже
        // ориентирован правильно; <0 — инвертируем перпендикуляр
        if (bulge < 0)
        {
            perp = new Point(-perp.X, -perp.Y);
        }

        return new Point(mid.X + perp.X * s, mid.Y + perp.Y * s);
    }

    /// <summary>
    /// Создаёт один сегмент цепочки размеров (от точки A до точки B).
    /// horizontal = true — горизонтальная линия, текст над.
    /// horizontal = false — вертикальная линия, текст слева (повёрнут -90°).
    /// </summary>
    private void CreateChainSegment(
        double ax, double ay, double bx, double by,
        double tickLen, bool horizontal)
    {
        double length;
        double textX, textY, angle;

        if (horizontal)
        {
            length = Math.Abs(bx - ax);
            textX = (ax + bx) / 2.0;
            textY = ay - 8;
            angle = 0;
        }
        else
        {
            length = Math.Abs(by - ay);
            textX = ax - 8;
            textY = (ay + by) / 2.0;
            angle = -90;
        }

        var geom = new StreamGeometry();
        using (var ctx = geom.Open())
        {
            // Линия
            ctx.BeginFigure(new AvaloniaPoint(ax, ay), false);
            ctx.LineTo(new AvaloniaPoint(bx, by));
            ctx.EndFigure(false);
            // Засечки
            if (horizontal)
            {
                ctx.BeginFigure(new AvaloniaPoint(ax, ay - tickLen), false);
                ctx.LineTo(new AvaloniaPoint(ax, ay + tickLen));
                ctx.EndFigure(false);
                ctx.BeginFigure(new AvaloniaPoint(bx, by - tickLen), false);
                ctx.LineTo(new AvaloniaPoint(bx, by + tickLen));
                ctx.EndFigure(false);
            }
            else
            {
                ctx.BeginFigure(new AvaloniaPoint(ax - tickLen, ay), false);
                ctx.LineTo(new AvaloniaPoint(ax + tickLen, ay));
                ctx.EndFigure(false);
                ctx.BeginFigure(new AvaloniaPoint(bx - tickLen, by), false);
                ctx.LineTo(new AvaloniaPoint(bx + tickLen, by));
                ctx.EndFigure(false);
            }
        }

        var dim = new DimensionViewModel(geom, $"{length:F0}", textX, textY, angle, _selectCmd);
        Children.Add(dim);
        Dimensions.Add(dim);
    }

    /// <summary>
    /// Длина импоста — расстояние между концами central line.
    /// Если импост материализован (Width &gt; 0 и Board создан) — берётся
    /// максимум из длины доски и самой длинной грани PhysicalContour
    /// (то же, что в BuildTree для DisplayName).
    /// Если дуговой — длина дуги по Bulge.
    /// </summary>
    private static double GetImpostLength(Impost impost)
    {
        if (impost.Width > 0 && impost.Assembly.Board != null)
            return Math.Max(
                ContourMath.LongestEdgeLength(impost.Assembly.Board.PhysicalContour),
                impost.Assembly.Board.Length);

        var contour = impost.GetContour();
        if (contour.Vertices.Count < 2) return 0;

        var from = contour.Vertices[0].Position;
        var to = contour.Vertices[contour.Vertices.Count - 1].Position;
        var bulge = contour.Vertices[0].Bulge;

        if (Math.Abs(bulge) < GeometryConstants.Tolerance)
            return from.DistanceTo(to);

        var arc = BulgeMath.GetArcParameters(from, to, bulge);
        return arc.Radius * Math.Abs(arc.SweepAngle);
    }

    /// <summary>
    /// Контрольная точка дуги (середина дуги) по концам и Bulge.
    /// null, если сегмент прямой.
    /// </summary>
    private static Point? ComputeArcControlPoint(Point from, Point to, double bulge)
    {
        if (Math.Abs(bulge) < GeometryConstants.Tolerance) return null;

        var mid = new Point((from.X + to.X) / 2.0, (from.Y + to.Y) / 2.0);

        var ab = new Point(to.X - from.X, to.Y - from.Y);
        double abLen = Math.Sqrt(ab.X * ab.X + ab.Y * ab.Y);
        if (abLen < 1e-9) return null;

        var perp = new Point(ab.Y / abLen, -ab.X / abLen);
        double s = abLen * bulge / 2.0;

        return new Point(mid.X + perp.X * s, mid.Y + perp.Y * s);
    }

    public override void RefreshTreeChildren()
    {
        TreeChildren = Children
            .Where(c => c is LightSpaceViewModel or SashViewModel or
                            BoardViewModel or JambViewModel)
            .ToList();
    }
}
