using BlueprintMaker.Application;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Presentation.Avalonia.Helpers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Media;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

/// <summary>
/// Главная ViewModel. Разбита на части:
///   MainViewModel.cs          — состояние, команды, конструктор, выбор элементов
///   MainViewModel.Editing.cs  — фабрики команд редактирования
///   MainViewModel.Document.cs — создание, открытие, сохранение, автосохранение
///   MainViewModel.Canvas.cs   — размер холста, центрирование, сетка
/// </summary>
public partial class MainViewModel : ReactiveObject
{
    private const double DefaultWidth = 20;
    private const double MaxMaterialWidth = 100;

    // Документ и история заменяются целиком при создании и открытии окна
    private Document _document = new();
    private DocumentHistory _history = new();

    // Поток, который срабатывает при любом изменении модели
    private readonly Subject<Unit> _modelChanged = new();

    // Состояние
    [Reactive] private bool _canUndo;
    [Reactive] private bool _canRedo;
    [Reactive] private string _statusMessage = "Готово";

    [Reactive] private FrameViewModel? _selectedFrame;
    [Reactive] private SelectableElement? _selectedElement;
    [Reactive] private Point? _lastClickPoint;

    [Reactive] private double _canvasWidth;
    [Reactive] private double _canvasHeight;
    [Reactive] private StreamGeometry? _gridGeometry;

    [Reactive] private string? _currentPath;
    [Reactive] private bool _isDirty;

    public ObservableCollection<FrameViewModel> Frames { get; } = new();

    public string WindowTitle =>
        $"{(CurrentPath != null ? System.IO.Path.GetFileName(CurrentPath) : "Без имени")}" +
        $"{(IsDirty ? " *" : "")} — BlueprintMaker";

    // Диалоги (показывает MainWindow)
    public Interaction<Unit, Contour?> AskNewWindow { get; } = new();
    /// <summary>Вход — предлагаемая ширина, выход — введённая (null — отмена).</summary>
    public Interaction<double, double?> AskJambWidth { get; } = new();
    public Interaction<string, ChoiceResult> AskUnsaved { get; } = new();
    public Interaction<string, ChoiceResult> AskRecover { get; } = new();
    public Interaction<Unit, string?> AskOpenPath { get; } = new();
    public Interaction<string, string?> AskSavePath { get; } = new();

    // Команды
    public ReactiveCommand<Unit, Unit> CreateWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveCommand { get; }
    public ReactiveCommand<Unit, Unit> SaveAsCommand { get; }

    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }

    public ReactiveCommand<SelectableElement, Unit> SelectElementCommand { get; }
    public ReactiveCommand<decimal, Unit> MaterializeCommand { get; }

    public Interaction<MaterializeOptions, MaterializeOptions?> AskMaterialize { get; } = new();
    public ReactiveCommand<Unit, Unit> MaterializeAllCommand { get; }

    public ReactiveCommand<Unit, Unit> InsertVerticalImpostCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertHorizontalImpostCommand { get; }
    public ReactiveCommand<decimal, Unit> InsertMultipleVerticalImpostsCommand { get; }
    public ReactiveCommand<decimal, Unit> InsertMultipleHorizontalImpostsCommand { get; }
    public ReactiveCommand<decimal, Unit> InsertSashCommand { get; }
    public ReactiveCommand<JambSlot, Unit> AddJambCmd { get; }

    public ReactiveCommand<Unit, Unit> SplitEqualCommand { get; }
    public ReactiveCommand<Unit, Unit> SplitAtClickCommand { get; }
    public ReactiveCommand<Unit, Unit> SplitImpostEqualCommand { get; }
    public ReactiveCommand<Unit, Unit> SplitImpostAtClickCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleArcCommand { get; }

    public ReactiveCommand<JointEndType, Unit> SetStartTypeCommand { get; }
    public ReactiveCommand<JointEndType, Unit> SetEndTypeCommand { get; }
    public ReactiveCommand<JointEndType, Unit> SetJointTypeCommand { get; }

    public MainViewModel()
    {
        // Условия доступности
        var selected = this.WhenAnyValue(x => x.SelectedElement);
        var hasWindow = this.WhenAnyValue(x => x.SelectedFrame).Select(f => f != null);

        var canMaterialize = selected.Select(el => el != null);
        var canChangeJoint = selected.Select(el => el is BoardViewModel);
        var canChangeSpace = selected.Select(el => el is LightSpaceViewModel);
        var canSplitFrame = selected.Select(el => el is WireframeLine);
        var canSplitImpost = selected.Select(el => el is ImpostSegment);
        var canToggleArc = selected.Select(el => el is WireframeLine || el is ImpostSegment);

        // Подписки
        AppSettings.Instance
            .WhenAnyValue(x => x.IsGridVisible, x => x.GridSize, x => x.Zoom, x => x.PanX, x => x.PanY)
            .Subscribe(_ => UpdateGridGeometry());
        this.WhenAnyValue(x => x.CanvasWidth, x => x.CanvasHeight).Subscribe(_ => UpdateGridGeometry());

        selected.Subscribe(el => SelectedFrame?.SetJambTarget(ResolveNode(el)));

        this.WhenAnyValue(x => x.CurrentPath, x => x.IsDirty)
            .Subscribe(_ => this.RaisePropertyChanged(nameof(WindowTitle)));

        this.WhenAnyValue(x => x.CanUndo, x => x.CanRedo)
            .Subscribe(_ => _modelChanged.OnNext(Unit.Default));

        _modelChanged.Subscribe(_ =>
        {
            RefreshGeometry();
            UpdateDirty();
        });

        // Файл и документ
        CreateWindowCommand = ReactiveCommand.CreateFromTask(CreateWindowAsync);
        OpenCommand = ReactiveCommand.CreateFromTask(OpenAsync);
        SaveCommand = ReactiveCommand.CreateFromTask(async () => { await SaveAsync(false); }, hasWindow);
        SaveAsCommand = ReactiveCommand.CreateFromTask(async () => { await SaveAsync(true); }, hasWindow);

        ReportErrors(CreateWindowCommand);
        ReportErrors(OpenCommand);
        ReportErrors(SaveCommand);
        ReportErrors(SaveAsCommand);

        // История
        UndoCommand = ReactiveCommand.Create(() => StepHistory(undo: true), this.WhenAnyValue(x => x.CanUndo));
        RedoCommand = ReactiveCommand.Create(() => StepHistory(undo: false), this.WhenAnyValue(x => x.CanRedo));

        // Выбор и материализация
        SelectElementCommand = ReactiveCommand.Create<SelectableElement>(SelectElement);
        MaterializeCommand = CreateMaterializeCommand(canMaterialize);

        MaterializeAllCommand = ReactiveCommand.CreateFromTask(MaterializeAllAsync, hasWindow);
        ReportErrors(MaterializeAllCommand);

        // Вставка элементов
        InsertVerticalImpostCommand = CreateInsertImpostCommand(AxisOrientation.Vertical, canChangeSpace);
        InsertHorizontalImpostCommand = CreateInsertImpostCommand(AxisOrientation.Horizontal, canChangeSpace);
        InsertMultipleVerticalImpostsCommand = CreateInsertMultipleImpostsCommand(AxisOrientation.Vertical, canChangeSpace);
        InsertMultipleHorizontalImpostsCommand = CreateInsertMultipleImpostsCommand(AxisOrientation.Horizontal, canChangeSpace);
        InsertSashCommand = CreateInsertSashCommand(canChangeSpace);
        AddJambCmd = ReactiveCommand.CreateFromTask<JambSlot>(AddJambAsync);
        ReportErrors(AddJambCmd);

        // Линии, импосты, дуги
        SplitEqualCommand = CreateSplitFrameLineCommand(atClick: false, canSplitFrame);
        SplitAtClickCommand = CreateSplitFrameLineCommand(atClick: true, canSplitFrame);
        SplitImpostEqualCommand = CreateSplitImpostCommand(atClick: false, canSplitImpost);
        SplitImpostAtClickCommand = CreateSplitImpostCommand(atClick: true, canSplitImpost);
        ToggleArcCommand = ReactiveCommand.Create(ToggleArc, canToggleArc);

        // Стыки
        SetStartTypeCommand = CreateBoardJointCommand(atStart: true, canChangeJoint);
        SetEndTypeCommand = CreateBoardJointCommand(atStart: false, canChangeJoint);
        SetJointTypeCommand = ReactiveCommand.Create<JointEndType>(SetAllJoints);

        StartAutosave();
    }

    //  Выбор элементов

    private void SelectElement(SelectableElement element)
    {

        if (element is LightSpaceViewModel ls && ls.Children.Count > 0)
        {
            StatusMessage = "Световой проём занят — выберите вложенный элемент";
            return;
        }     

        if (element is ImpostViewModel impVM)
        {
            if (impVM.Segments.Count > 0)
                element = impVM.Segments[0];
            else if (impVM.Boards.Count > 0)
                element = impVM.Boards[0];
        }

        if (SelectedFrame != null) DeselectAll(SelectedFrame);
        element.IsSelected = true;
        SelectedElement = element;
        StatusMessage = $"Выбран: {element.DisplayName}";
    }

    public void Unselect()
    {
        SelectedElement = null;
        foreach (var frame in Frames)
            DeselectAll(frame);
    }

    private void DeselectAll(SelectableElement element)
    {
        element.IsSelected = false;
        foreach (var child in element.Children)
            DeselectAll(child);

        switch (element)
        {
            case ImpostViewModel imp:
                foreach (var seg in imp.Segments) DeselectAll(seg);
                foreach (var board in imp.Boards) DeselectAll(board);
                break;
            case SashViewModel sash:
                foreach (var board in sash.Boards) DeselectAll(board);
                break;
            case JambViewModel jamb:
                foreach (var board in jamb.Boards) DeselectAll(board);
                break;
        }
    }

    private static IWindowNode? ResolveNode(SelectableElement? el) => el switch
    {
        FrameViewModel f => f.Frame,
        SashViewModel s => s.SourceSash,
        ImpostViewModel i => i.SourceImpost,
        LightSpaceViewModel ls => ls.SourceSpace,
        _ => null
    };

    //  Общие помощники

    /// <summary>Выполняет команду через историю и обновляет модель, состояние истории и статус.</summary>
    private void Commit(IUndoableCommand command, string? status = null)
    {
        _history.Execute(command);
        _modelChanged.OnNext(Unit.Default);
        UpdateHistoryState();
        if (status != null) StatusMessage = status;
    }

    private void UpdateHistoryState()
    {
        CanUndo = _history.CanUndo;
        CanRedo = _history.CanRedo;
        UpdateDirty();
    }

    private void RefreshGeometry()
    {
        foreach (var frame in Frames)
            frame.Refresh();

        this.RaisePropertyChanged(nameof(Frames));
    }

    private void ReportErrors(IHandleObservableErrors command) =>
        command.ThrownExceptions.Subscribe(ex => StatusMessage = $"Ошибка: {ex.Message}");

    /// <summary>
    /// Выполняет Undo-команду через историю. Используется code-behind (MainWindow)
    /// для сохранения финального результата drag в истории.
    /// </summary>
    public void ExecuteCommand(IUndoableCommand command)
    {
        _history.Execute(command);
        UpdateHistoryState();
    }

    public void RecordCommand(IUndoableCommand command)
    {
        _history.Record(command);
        UpdateHistoryState();
    }
}