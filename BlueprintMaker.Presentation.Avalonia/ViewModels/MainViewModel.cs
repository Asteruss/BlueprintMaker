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
    public ReactiveCommand<Unit, Unit> UndoCommand { get; }
    public ReactiveCommand<Unit, Unit> RedoCommand { get; }
    public ReactiveCommand<Unit, Unit> MaterializeCommand { get; }
    public ReactiveCommand<JointEndType, Unit> SetJointTypeCommand { get; }

    public MainViewModel()
    {
        var canUndoObservable = this.WhenAnyValue(x => x.CanUndo);
        var canRedoObservable = this.WhenAnyValue(x => x.CanRedo);
        var canMaterializeObservable = this.WhenAnyValue(x => x.Frames.Count, count => count > 0);

        CreateWindowCommand = ReactiveCommand.Create(CreateWindow);

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
            Frames.Add(new FrameViewModel(_document.Root, _history));
            SelectedFrame = Frames[0];
        }

        StatusMessage = "Окно создано";
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
}