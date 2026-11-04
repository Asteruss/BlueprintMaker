using BlueprintMaker.Application;
using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Presentation.Avalonia.Helpers;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class MainViewModel
{
    private double _lastJambWidth = DefaultWidth;

    private const string SelectSpaceHint = "Выберите световое пространство";

    /// <summary>Ширина из поля: значения вне (0; 50] заменяются на значение по умолчанию.</summary>
    private static double NormalizeWidth(decimal input) =>
        input <= 0 || input > (decimal)MaxMaterialWidth ? DefaultWidth : (double)input;

    // История

    private void StepHistory(bool undo)
    {
        if (undo) _history.Undo();
        else _history.Redo();

        _modelChanged.OnNext(Unit.Default);
        UpdateHistoryState();
    }

    // Материализация

    private ReactiveCommand<decimal, Unit> CreateMaterializeCommand(IObservable<bool> canExecute) =>
        ReactiveCommand.Create<decimal>(sizeInput =>
        {
            if (_document.Root == null) return;

            double size = NormalizeWidth(sizeInput);

            PhysicalElement element = SelectedElement switch
            {
                ImpostViewModel iv => iv.SourceImpost,
                ImpostSegment seg when seg.SourceImpost is Impost imp => imp,
                _ => _document.Root
            };

            if (element == _document.Root && Frames.Count > 0)
                SelectedFrame = Frames[0];

            Commit(new MaterializeCommand(element, size), $"Материализован ({size}мм)");
        }, canExecute);

    // Вставка элементов

    private ReactiveCommand<Unit, Unit> CreateInsertImpostCommand(
        AxisOrientation orientation, IObservable<bool> canExecute) =>
        ReactiveCommand.Create(() =>
        {
            if (SelectedElement is not LightSpaceViewModel space)
            {
                StatusMessage = SelectSpaceHint;
                return;
            }

            Commit(new InsertImpostCommand(space.SourceSpace, orientation, 0.5), "Импост вставлен");
            SelectedElement = null;
        }, canExecute);

    private ReactiveCommand<decimal, Unit> CreateInsertMultipleImpostsCommand(
        AxisOrientation orientation, IObservable<bool> canExecute) =>
        ReactiveCommand.Create<decimal>(countInput =>
        {
            if (SelectedElement is not LightSpaceViewModel space)
            {
                StatusMessage = SelectSpaceHint;
                return;
            }

            int count = (int)countInput;
            Commit(new InsertMultipleImpostsCommand(space.SourceSpace, count, orientation),
                   $"Вставлено {count} импостов");
        }, canExecute);

    private ReactiveCommand<decimal, Unit> CreateInsertSashCommand(IObservable<bool> canExecute) =>
        ReactiveCommand.Create<decimal>(sizeInput =>
        {
            if (SelectedElement is not LightSpaceViewModel space)
            {
                StatusMessage = SelectSpaceHint;
                return;
            }

            double width = NormalizeWidth(sizeInput);
            Commit(new InsertSashCommand(space.SourceSpace, width), $"Створка вставлена ({width}мм)");
        }, canExecute);

    private async Task AddJambAsync(JambSlot slot)
    {
        var width = await AskJambWidth.Handle(_lastJambWidth);

        if (width is not > 0) return;   // отмена

        _lastJambWidth = width.Value;
        Commit(new AddJambCommand(slot.Wall, slot.SegmentIndex, slot.Space, width.Value),
               $"Добор добавлен ({width.Value:0.##} мм)");
    }

    // Разбиение линий и импостов

    private ReactiveCommand<Unit, Unit> CreateSplitFrameLineCommand(bool atClick, IObservable<bool> canExecute) =>
        ReactiveCommand.Create(() =>
        {
            if (SelectedElement is not WireframeLine line)
            {
                StatusMessage = "Выберите линию для разбиения";
                return;
            }
            if (atClick && LastClickPoint == null)
            {
                StatusMessage = "Сначала кликните по линии, чтобы выбрать её";
                return;
            }

            var point = atClick ? LastClickPoint : null;
            Commit(new SplitSegmentCommand(line.SourceElement, line.SegmentIndex, point),
                   atClick ? "Линия разбита в точке клика" : "Линия разбита поровну");
            if (atClick) LastClickPoint = null;
        }, canExecute);

    private ReactiveCommand<Unit, Unit> CreateSplitImpostCommand(bool atClick, IObservable<bool> canExecute) =>
        ReactiveCommand.Create(() =>
        {
            if (SelectedElement is not ImpostSegment seg || seg.SourceImpost is not Impost imp) return;
            if (atClick && LastClickPoint == null) return;

            var point = atClick ? LastClickPoint : null;
            Commit(new SplitImpostCommand(imp, seg.SegmentIndex, point),
                   atClick ? "Сегмент импоста разбит в точке клика" : "Сегмент импоста разбит поровну");
            if (atClick) LastClickPoint = null;
        }, canExecute);

    // Дуги
    private void ToggleArc()
    {
        PhysicalElement element;
        int segmentIndex;
        double currentBulge;

        switch (SelectedElement)
        {
            case WireframeLine line:
                element = line.SourceElement;
                segmentIndex = line.SegmentIndex;
                currentBulge = line.Bulge;
                break;
            case ImpostSegment seg:
                element = seg.SourceImpost;
                segmentIndex = seg.SegmentIndex;
                currentBulge = seg.Bulge;
                break;
            default:
                return;
        }

        double newBulge = Math.Abs(currentBulge) < 0.0001 ? 0.5 : 0.0;
        Commit(new SetSegmentArcCommand(element, segmentIndex, newBulge),
               newBulge == 0 ? "Сегмент стал прямой" : "Сегмент стал дугой");
    }

    // Стыки

    private ReactiveCommand<JointEndType, Unit> CreateBoardJointCommand(bool atStart, IObservable<bool> canExecute) =>
        ReactiveCommand.Create<JointEndType>(type =>
        {
            if (SelectedElement is not BoardViewModel bvm) return;

            var board = bvm.SourceBoard;
            var joint = atStart ? board.StartJoint : board.EndJoint;
            if (joint == null) return;   // у досок импоста и добора стыков нет

            Commit(new ChangeJointCommand(joint, type, forA: joint.BoardA == board));
        }, canExecute);

    private void SetAllJoints(JointEndType type)
    {
        if (SelectedFrame == null) return;

        var joints = SelectedFrame.Assembly.Joints;
        var steps = new List<IUndoableCommand>(joints.Count);
        foreach (var joint in joints)
            steps.Add(new ChangeJointCommand(joint, type, forA: true));

        Commit(new MacroCommand(steps), $"Применен стык: {type}");
    }

    private MaterializeOptions _lastMaterialize = new(DefaultWidth, false);

    private async Task MaterializeAllAsync()
    {
        if (_document.Root == null) return;

        var options = await AskMaterialize.Handle(_lastMaterialize);
        if (options == null) return;   // отмена
        _lastMaterialize = options;

        var command = MaterializeTreeCommand.Create(
            _document.Root, options.Width, options.OverwriteExisting, out int skipped);

        if (command.Count == 0)
        {
            StatusMessage = skipped > 0
                ? "Все элементы уже материализованы. Включите «Изменять уже материализованные»."
                : "Нечего материализовывать.";
            return;
        }

        Commit(command,
            $"Материализовано: {command.Count} эл., ширина {options.Width:0.##} мм" +
            (skipped > 0 ? $", пропущено {skipped}" : ""));
    }

    private static bool IsDeletable(SelectableElement? el) => el is
    ImpostViewModel or SashViewModel or JambViewModel or
    FrameViewModel or LightSpaceViewModel or BoardViewModel or ImpostSegment;

    private static IWindowNode? ResolveDeleteTarget(SelectableElement? el) => el switch
    {
        ImpostViewModel i => i.SourceImpost,
        SashViewModel s => s.SourceSash,
        JambViewModel j => j.SourceJamb,
        FrameViewModel f => f.Frame,
        LightSpaceViewModel ls => ls.SourceSpace,
        BoardViewModel b => b.Owner,
        ImpostSegment i => i.SourceImpost,
        _ => null
    };

    private async Task DeleteSelectedAsync()
    {
        var target = ResolveDeleteTarget(SelectedElement);
        if (target == null)
        {
            StatusMessage = "Не удалось определить элемент для удаления";
            return;
        }

        var plan = ElementRemoval.Plan(target);
        if (!plan.CanRemove)
        {
            StatusMessage = plan.BlockReason!;
            await ShowMessage.Handle(plan.BlockReason!);
            return;
        }

        var name = ElementRemoval.Describe(target);
        Unselect();   // до удаления, чтобы не осталось ссылок на удалённые элементы
        Commit(new DeleteElementCommand(plan), $"Удалено: {name}");
    }
}