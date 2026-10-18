using BlueprintMaker.Application;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class MainViewModel : ReactiveObject
{
    private readonly Document _document = new();
    private readonly DocumentHistory _history = new();

    [Reactive] private bool _canUndo;
    [Reactive] private bool _canRedo;
    [Reactive] private string _statusMessage = "Готово";
    [Reactive] private FrameViewModel? _selectedFrame;

    public ObservableCollection<FrameViewModel> Frames { get; } = new();

    public ReactiveCommand<Unit, Unit> CreateWindowCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateWindowTCommand { get; }
    public ReactiveCommand<Unit, Unit> CreateWindowHCommand { get; }
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }
    public ReactiveCommand<Unit, Unit> MaterializeCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertVerticalImpostCommand { get; }
    public ReactiveCommand<Unit, Unit> InsertHorizontalImpostCommand { get; }
    public ReactiveCommand<SelectableElement, Unit> SelectElementCommand { get; }
    public ReactiveCommand<JointEndType, Unit> SetJointTypeCommand { get; }
    [Reactive] private SelectableElement? _selectedElement;
    public MainViewModel()
    {
        var canUndoObservable = this.WhenAnyValue(x => x.CanUndo);
        var canRedoObservable = this.WhenAnyValue(x => x.CanRedo);
        var canMaterializeObservable = this.WhenAnyValue(x => x.Frames.Count, count => count > 0);

        CreateWindowCommand = ReactiveCommand.Create(CreateWindow);
        CreateWindowTCommand = ReactiveCommand.Create(CreateTrapezoidWindow);
        CreateWindowHCommand = ReactiveCommand.Create(CreateHexagonWindow);

        SelectElementCommand = ReactiveCommand.Create<SelectableElement>(element =>
        {
            // Рекурсивно снимаем выделение со всего дерева
            if (SelectedFrame != null)
            {
                DeselectAll(SelectedFrame);
            }

            // Выделяем только выбранный
            element.IsSelected = true;
            SelectedElement = element;
            StatusMessage = $"Выбран: {element.DisplayName}";
        });

        UndoCommand = ReactiveCommand.Create(() =>
        {
            _history.Undo();
            UpdateHistoryState();
        }, canUndoObservable);

        RedoCommand = ReactiveCommand.Create(() =>
        {
            _history.Redo();
            UpdateHistoryState();
        }, canRedoObservable);

        MaterializeCommand = ReactiveCommand.Create(() =>
        {
            if (_document.Root != null)
            {
                _document.Root.Materialize(50);
                _statusMessage = "Рама материализована (50мм)";

                // Выделяем раму для инструмента стыков
                if (Frames.Count > 0)
                    SelectedFrame = Frames[0];

                UpdateHistoryState();
            }
        }, canMaterializeObservable);

        InsertVerticalImpostCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is LightSpaceViewModel lsVM)
            {
                _history.Execute(new InsertImpostCommand(lsVM.SourceSpace, Core.Geometry.AxisOrientation.Vertical, 0.5));
                SelectedFrame?.Refresh();
                SelectedElement = null;
                StatusMessage = "Импост вставлен";
            }
            else
            {
                StatusMessage = "Выберите световое пространство для импоста";
            }
        }, this.WhenAnyValue(x => x.SelectedElement).Select(el => el is LightSpaceViewModel)); 

        InsertHorizontalImpostCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedElement is LightSpaceViewModel lsVM)
            {
                _history.Execute(new InsertImpostCommand(lsVM.SourceSpace, Core.Geometry.AxisOrientation.Horizontal, 0.5));
                SelectedFrame?.Refresh();
                SelectedElement = null;

                StatusMessage = "Импост вставлен";
            }
            else
            {
                StatusMessage = "Выберите световое пространство для импоста";
            }
        }, this.WhenAnyValue(x => x.SelectedElement).Select(el => el is LightSpaceViewModel));

        // Команда смены стыка
        SetJointTypeCommand = ReactiveCommand.Create<JointEndType>(type =>
        {
            if (SelectedFrame == null) return;

            var assembly = SelectedFrame.Assembly;
            for (int i = 0; i < assembly.Joints.Count; i++)
            {
                assembly.ChangeJoint(i, type);
            }

            // Принудительно перерисовываем раму
            SelectedFrame.Refresh();
            _statusMessage = $"Применен стык: {type}";
        });

        this.WhenAnyValue(x => x.CanUndo, x => x.CanRedo)
            .Subscribe(_ => RefreshGeometry());
    }

    private void CreateWindow()
    {
        var vertices = new Point[]
        {
            new(50, 50), new(550, 50), new(550, 450), new(50, 450)
        };

        var contour = Contour.CreateClosed(new[]
        {
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
    }
    private void CreateTrapezoidWindow()
    {
        // Рассчитываем координаты углов трапеции
        var vertices = new Point[]
        {
        new(150, 50),   // Верхний левый угол (сдвинут вправо)
        new(450, 50),   // Upper правый угол (сдвинут влевую сторону)
        new(550, 450),  // Нижний правый угол
        new(50, 450)    // Нижний левый угол
        };

        var contour = Contour.CreateClosed(new[]
        {
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
    }
    private void CreateHexagonWindow()
    {
        // Центр и радиус для правильного шестиугольника
        double centerX = 300;
        double centerY = 250;
        double radius = 200;

        var vertices = new Point[6];

        // Шаг угла для 6-угольника равен 60 градусам (PI / 3 радиан)
        for (int i = 0; i < 6; i++)
        {
            double angle = i * Math.PI / 3;

            // Округляем до 2 знаков, чтобы избежать микро-погрешностей double
            double x = Math.Round(centerX + radius * Math.Cos(angle), 2);
            double y = Math.Round(centerY + radius * Math.Sin(angle), 2);

            vertices[i] = new Point(x, y);
        }

        var contour = Contour.CreateClosed(new[]
        {
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
        {
            DeselectAll(child);
        }
    }
}