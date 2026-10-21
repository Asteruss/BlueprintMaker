using BlueprintMaker.Application;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class MainViewModel : ReactiveObject
{
    private readonly Document _document = new();
    private readonly DocumentHistory _history = new();

    // Реактивный поток, который будет дергаться при любом изменении модели
    private readonly Subject<Unit> _modelChanged = new();

    [Reactive] private bool _canUndo;
    [Reactive] private bool _canRedo;
    [Reactive] private string _statusMessage = "Готово";
    [Reactive] private bool _isNodeVisible = true;

    [Reactive] private FrameViewModel? _selectedFrame;
    [Reactive] private Point? _lastClickPoint;
    [Reactive] private SelectableElement? _selectedElement;

    public ObservableCollection<FrameViewModel> Frames { get; } = new();

    public ReactiveCommand<Unit, Unit> CreateWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateOvalWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateWindowTCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateWindowHCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }
    public ReactiveCommand<decimal, Unit> MaterializeCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertVerticalImpostCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertHorizontalImpostCommand { get; }
    public ReactiveCommand<decimal, Unit> InsertMultipleVerticalImpostsCommand { get; }
    public ReactiveCommand<decimal, Unit> InsertMultipleHorizontalImpostsCommand { get; }
    public ReactiveCommand<decimal, Unit> InsertSashCommand { get; }
    public ReactiveCommand<Unit, Unit> SplitEqualCommand { get; }
    public ReactiveCommand<Unit, Unit> SplitAtClickCommand { get; }
    public ReactiveCommand<JointEndType, Unit> SetStartTypeCommand { get; }
    public ReactiveCommand<JointEndType, Unit> SetEndTypeCommand { get; }
    public ReactiveCommand<SelectableElement, Unit> SelectElementCommand { get; }
    public ReactiveCommand<JointEndType, Unit> SetJointTypeCommand { get; }
    public ReactiveCommand<Unit, Unit> SplitImpostEqualCommand { get; }
    public ReactiveCommand<Unit, Unit> SplitImpostAtClickCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleArcCommand { get; }

    public MainViewModel()
    {
        var canUndoObservable = this.WhenAnyValue(x => x.CanUndo);
        var canRedoObservable = this.WhenAnyValue(x => x.CanRedo);
        var canMaterializeObservable = this.WhenAnyValue(x => x.SelectedElement).Select(el => el is SelectableElement);
        var canChangeJoint = this.WhenAnyValue(x => x.SelectedElement).Select(el => el is BoardViewModel);
        var canChangeSpace = this.WhenAnyValue(x => x.SelectedElement).Select(el => el is LightSpaceViewModel);
        var canSplitImpost = this.WhenAnyValue(x => x.SelectedElement).Select(el => el is ImpostSegment);
        var canSplitFrmae = this.WhenAnyValue(x => x.SelectedElement).Select(el => el is WireframeLine);
        var canToggleArc = this.WhenAnyValue(x => x.SelectedElement).Select(el => el is WireframeLine || el is ImpostSegment);
        var canInsertSash = this.WhenAnyValue(x => x.SelectedElement).Select(el => el is LightSpaceViewModel);


        _modelChanged.Subscribe(_ => RefreshGeometry());

        CreateWindowCommand = ReactiveCommand.Create(CreateWindow);
        CreateWindowTCommand = ReactiveCommand.Create(CreateTrapezoidWindow);
        CreateWindowHCommand = ReactiveCommand.Create(CreateHexagonWindow);
        CreateOvalWindowCommand = ReactiveCommand.Create(CreateOvalWindow);

        SetStartTypeCommand = ReactiveCommand.Create<JointEndType>(type =>
        {
            if (SelectedElement is BoardViewModel bvm)
            {
                var board = bvm.SourceBoard;
                var joint = board.StartJoint;
                if (joint != null)
                {
                    bool forA = joint.BoardA == board;
                    _history.Execute(new ChangeJointCommand(joint, type, forA));
                    _modelChanged.OnNext(Unit.Default);
                }
            }
        }, canChangeJoint);

        SetEndTypeCommand = ReactiveCommand.Create<JointEndType>(type =>
        {
            if (SelectedElement is BoardViewModel bvm)
            {
                var board = bvm.SourceBoard;
                var joint = board.EndJoint;
                if (joint != null)
                {
                    bool forA = joint.BoardA == board;
                    _history.Execute(new ChangeJointCommand(joint, type, forA));
                    _modelChanged.OnNext(Unit.Default);
                }
            }
        }, canChangeJoint);

        SelectElementCommand = ReactiveCommand.Create<SelectableElement>(element =>
        {
            if (SelectedFrame != null) DeselectAll(SelectedFrame);
            element.IsSelected = true;
            SelectedElement = element;
            StatusMessage = $"Выбран: {element.DisplayName}";
        });

        InsertMultipleVerticalImpostsCommand = ReactiveCommand.Create<decimal>(countDecimal =>
        {
            if (SelectedElement is LightSpaceViewModel lsVM)
            {
                int count = (int)countDecimal;
                _history.Execute(new InsertMultipleImpostsCommand(lsVM.SourceSpace, count, AxisOrientation.Vertical));
                _modelChanged.OnNext(Unit.Default);
                StatusMessage = $"Вставлено {count} импостов";
            }
            else StatusMessage = "Выберите световое пространство";
        }, canChangeSpace);


        InsertMultipleHorizontalImpostsCommand = ReactiveCommand.Create<decimal>(countDecimal =>
        {
            if (SelectedElement is LightSpaceViewModel lsVM)
            {
                int count = (int)countDecimal;
                _history.Execute(new InsertMultipleImpostsCommand(lsVM.SourceSpace, count, AxisOrientation.Horizontal));
                _modelChanged.OnNext(Unit.Default);
                StatusMessage = $"Вставлено {count} импостов";
            }
            else StatusMessage = "Выберите световое пространство";
        }, canChangeSpace);

        SplitAtClickCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is WireframeLine line && LastClickPoint != null)
            {
                _history.Execute(new SplitSegmentCommand(line.SourceElement, line.SegmentIndex, LastClickPoint));
                _modelChanged.OnNext(Unit.Default);
                StatusMessage = "Линия разбита в точке клика";
                LastClickPoint = null;
            }
            else StatusMessage = "Сначала кликните по линии, чтобы выбрать её";
        }, canSplitFrmae);

        SplitEqualCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is WireframeLine line)
            {
                _history.Execute(new SplitSegmentCommand(line.SourceElement, line.SegmentIndex, null));
                _modelChanged.OnNext(Unit.Default);
                StatusMessage = "Линия разбита поровну";
            }
            else StatusMessage = "Выберите линию для разбиения";
        }, canSplitFrmae);

        SplitImpostEqualCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is ImpostSegment seg && seg.SourceImpost is Impost imp)
            {
                _history.Execute(new SplitImpostCommand(imp, seg.SegmentIndex, null));
                _modelChanged.OnNext(Unit.Default);
                StatusMessage = "Сегмент импоста разбит поровну";
            }
        }, canSplitImpost);

        SplitImpostAtClickCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is ImpostSegment seg && seg.SourceImpost is Impost imp && LastClickPoint != null)
            {
                _history.Execute(new SplitImpostCommand(imp, seg.SegmentIndex, LastClickPoint));
                LastClickPoint = null;
                _modelChanged.OnNext(Unit.Default);
                StatusMessage = "Сегмент импоста разбит в точке клика";
            }

        }, canSplitImpost);

        UndoCommand = ReactiveCommand.Create(() =>
        {
            _history.Undo();
            _modelChanged.OnNext(Unit.Default);
            UpdateHistoryState();
        }, canUndoObservable);

        RedoCommand = ReactiveCommand.Create(() =>
        {
            _history.Redo();
            _modelChanged.OnNext(Unit.Default);
            UpdateHistoryState();
        }, canRedoObservable);

        MaterializeCommand = ReactiveCommand.Create<decimal>(sizeInput =>
        {
            if (_document.Root != null)
            {
                double size = (sizeInput < 0 || sizeInput > 50) ? 20 : (double)sizeInput;
                PhysicalElement element;
                if (SelectedElement is ImpostViewModel impVM)
                    element = impVM.SourceImpost;
                else if (SelectedElement is ImpostSegment impSeg && impSeg.SourceImpost is Impost imp)
                    element = imp;
                else
                    element = _document.Root;

                _history.Execute(new MaterializeCommand(element, size));
                if (element == _document.Root && Frames.Count > 0)
                    SelectedFrame = Frames[0];

                _modelChanged.OnNext(Unit.Default);
                UpdateHistoryState();
                StatusMessage = $"Материализован ({size}мм)";
            }
        }, canMaterializeObservable);


        InsertSashCommand = ReactiveCommand.Create<decimal>(sizeInput =>
        {
            if (SelectedElement is LightSpaceViewModel lsVM)
            {
                double sashWidth = (sizeInput < 0 || sizeInput > 50) ? 20 : (double)sizeInput;
                _history.Execute(new InsertSashCommand(lsVM.SourceSpace, sashWidth));
                _modelChanged.OnNext(Unit.Default);
                StatusMessage = $"Створка вставлена ({sashWidth}мм)";
            }
            else StatusMessage = "Выберите световое пространство";
        }, canInsertSash);

        ToggleArcCommand = ReactiveCommand.Create(() =>
        {
            PhysicalElement element = null!;
            int segmentIndex = 0;
            double currentBulge = 0;

            if (SelectedElement is WireframeLine line)
            {
                element = line.SourceElement;
                segmentIndex = line.SegmentIndex;
                currentBulge = line.Bulge;
            }
            else if (SelectedElement is ImpostSegment seg)
            {
                element = seg.SourceImpost;
                segmentIndex = seg.SegmentIndex;
                currentBulge = seg.Bulge;
            }
            else return;

            var newBulge = Math.Abs(currentBulge) < 0.0001 ? 0.5 : 0.0;
            _history.Execute(new SetSegmentArcCommand(element, segmentIndex, newBulge));
            _modelChanged.OnNext(Unit.Default);
            StatusMessage = newBulge == 0 ? "Сегмент стал прямой" : "Сегмент стал дугой";
        }, canToggleArc);

        InsertVerticalImpostCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is LightSpaceViewModel lsVM)
            {
                _history.Execute(new InsertImpostCommand(lsVM.SourceSpace, Core.Geometry.AxisOrientation.Vertical, 0.5));
                _modelChanged.OnNext(Unit.Default);
                SelectedElement = null;
                StatusMessage = "Импост вставлен";
            }
            else StatusMessage = "Выберите световое пространство";
        }, canChangeSpace);

        InsertHorizontalImpostCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is LightSpaceViewModel lsVM)
            {
                _history.Execute(new InsertImpostCommand(lsVM.SourceSpace, Core.Geometry.AxisOrientation.Horizontal, 0.5));
                _modelChanged.OnNext(Unit.Default);
                SelectedElement = null;
                StatusMessage = "Импост вставлен";
            }
            else StatusMessage = "Выберите световое пространство";
        }, canChangeSpace);

        SetJointTypeCommand = ReactiveCommand.Create<JointEndType>(type =>
        {
            if (SelectedFrame == null) return;
            var assembly = SelectedFrame.Assembly;
            var steps = new List<IUndoableCommand>();
            for (int i = 0; i < assembly.Joints.Count; i++)
                steps.Add(new ChangeJointCommand(assembly.Joints[i], type, forA: true));
            _history.Execute(new MacroCommand(steps));
            _modelChanged.OnNext(Unit.Default);
            StatusMessage = $"Применен стык: {type}";
        });


        this.WhenAnyValue(x => x.CanUndo, x => x.CanRedo)
            .Subscribe(_ => _modelChanged.OnNext(Unit.Default));
    }

    private void CreateWindow()
    {
        var vertices = new Point[] { new(50, 50), new(550, 50), new(550, 450), new(50, 450) };
        var contour = Contour.CreateClosed(new[] {
            new ContourVertex(vertices[0]), new ContourVertex(vertices[1]),
            new ContourVertex(vertices[2]), new ContourVertex(vertices[3])
        });

        _history.Execute(new CreateFrameCommand(_document, contour));
        Frames.Clear();
        if (_document.Root != null)
        {
            Frames.Add(new FrameViewModel(_document.Root, _history, SelectElementCommand));
            SelectedFrame = Frames[0];
            SelectedFrame.IsSelected = true;
        }
        StatusMessage = "Окно создано";
        UpdateHistoryState();
        _modelChanged.OnNext(Unit.Default);
    }

    private void CreateTrapezoidWindow()
    {
        var vertices = new Point[] {
            new(150, 50), new(450, 50), new(550, 450), new(50, 450)
        };
        var contour = Contour.CreateClosed(new[] {
            new ContourVertex(vertices[0]), new ContourVertex(vertices[1]),
            new ContourVertex(vertices[2]), new ContourVertex(vertices[3])
        });

        _history.Execute(new CreateFrameCommand(_document, contour));
        Frames.Clear();
        if (_document.Root != null)
        {
            Frames.Add(new FrameViewModel(_document.Root, _history, SelectElementCommand));
            SelectedFrame = Frames[0];
            SelectedFrame.IsSelected = true;
        }
        StatusMessage = "Окно-трапеция создано";
        UpdateHistoryState();
        _modelChanged.OnNext(Unit.Default);
    }

    private void CreateHexagonWindow()
    {
        double centerX = 300, centerY = 250, radius = 200;
        var vertices = new Point[6];
        for (int i = 0; i < 6; i++)
        {
            double angle = i * Math.PI / 3;
            double x = Math.Round(centerX + radius * Math.Cos(angle), 2);
            double y = Math.Round(centerY + radius * Math.Sin(angle), 2);
            vertices[i] = new Point(x, y);
        }
        var contour = Contour.CreateClosed(new[] {
            new ContourVertex(vertices[0]), new ContourVertex(vertices[1]),
            new ContourVertex(vertices[2]), new ContourVertex(vertices[3]),
            new ContourVertex(vertices[4]), new ContourVertex(vertices[5])
        });

        _history.Execute(new CreateFrameCommand(_document, contour));
        Frames.Clear();
        if (_document.Root != null)
        {
            Frames.Add(new FrameViewModel(_document.Root, _history, SelectElementCommand));
            SelectedFrame = Frames[0];
            SelectedFrame.IsSelected = true;
        }
        StatusMessage = "Правильный шестиугольник создан";
        UpdateHistoryState();
        _modelChanged.OnNext(Unit.Default);
    }

    private void CreateOvalWindow()
    {
        double centerX = 300, centerY = 250;
        double a = 250;  // горизонтальная полуось
        double b = 200;  // вертикальная полуось

        var vertices = new Point[] {
        new(centerX + a, centerY),  // правая
        new(centerX, centerY - b),  // верхняя
        new(centerX - a, centerY),  // левая
        new(centerX, centerY + b),  // нижняя
    };

        double bulge = -Math.Tan(Math.PI / 8);

        var contour = Contour.CreateClosed(new[] {
        new ContourVertex(vertices[0], bulge),
        new ContourVertex(vertices[1], bulge),
        new ContourVertex(vertices[2], bulge),
        new ContourVertex(vertices[3], bulge),
    });

        _history.Execute(new CreateFrameCommand(_document, contour));
        Frames.Clear();
        if (_document.Root != null)
        {
            Frames.Add(new FrameViewModel(_document.Root, _history, SelectElementCommand));
            SelectedFrame = Frames[0];
            SelectedFrame.IsSelected = true;
        }
        StatusMessage = "Овальное окно создано";
        UpdateHistoryState();
        _modelChanged.OnNext(Unit.Default);
    }


    private void UpdateHistoryState()
    {
        CanUndo = _history.CanUndo;
        CanRedo = _history.CanRedo;
    }

    private void RefreshGeometry()
    {
        foreach (var frame in Frames)
            frame.Refresh();

        this.RaisePropertyChanged(nameof(Frames));
    }

    private void DeselectAll(SelectableElement element)
    {
        element.IsSelected = false;
        foreach (var child in element.Children)
            DeselectAll(child);

        if (element is ImpostViewModel imp)
            foreach (var seg in imp.Segments)
                DeselectAll(seg);

        if (element is SashViewModel sash)
            foreach (var board in sash.Boards)
                DeselectAll(board);
    }

    /// <summary>
    /// Выполняет Undo-команду через историю. Используется code-behind (MainWindow)
    /// для сохранения финального результата drag в истории.
    /// </summary>
    public void ExecuteCommand(IUndoableCommand command)
    {
        _history.Execute(command);
        UpdateHistoryState();
    }
}