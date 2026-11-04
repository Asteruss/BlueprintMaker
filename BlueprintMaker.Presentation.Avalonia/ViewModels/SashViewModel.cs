using Avalonia.Media;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Presentation.Avalonia.Helpers;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

/// <summary>
/// ViewModel для створки. В отличие от FrameViewModel, не имеет Lines и Nodes —
/// форму створки менять нельзя
/// </summary>
public partial class SashViewModel : SelectableElement
{
    public Sash SourceSash { get; }

    /// <summary>Доски створки — для выбора и смены соединений.</summary>
    public ObservableCollection<BoardViewModel> Boards { get; } = new();

    /// <summary>Доступ к Assembly — для совместимости с FrameViewModel-логикой.</summary>
    public FrameAssembly Assembly => SourceSash.Assembly;

    [Reactive] private bool _isMaterialized;

    public SashViewModel(Sash sash, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceSash = sash;
        DisplayName = "Створка";
        IsMaterialized = sash.Width > 0;
    }
    public IBrush FillColor => IsSelected ? Brushes.Blue : Brushes.Black;

    /// <summary>
    /// Перестроение досок створки. Вызывается из FrameViewModel.Refresh()
    /// при изменении модели. У створки нет Lines/Nodes — обновляем только Boards
    /// </summary>
    public void RefreshBoards()
    {
        int selectedIndex = -1;
        for (int i = 0; i < Boards.Count; i++)
            if (Boards[i].IsSelected) { selectedIndex = i; break; }

        // Очистить старые Board'ы из Children
        for (int i = Children.Count - 1; i >= 0; i--)
            if (Children[i] is BoardViewModel)
                Children.RemoveAt(i);

        Boards.Clear();
        IsMaterialized = SourceSash.Width > 0;

        if (SourceSash.Width > 0)
        {
            SourceSash.Assembly.Rebuild(
                SourceSash.GetContour(),
                SourceSash.InnerSpace.GetContour(),
                SourceSash.Width);

            var bounds = SourceSash.GetContour().BoundingBox;
            var center = new Core.Geometry.Point(
                (bounds.MinX + bounds.MaxX) / 2.0,
                (bounds.MinY + bounds.MaxY) / 2.0);

            for (int i = 0; i < SourceSash.Assembly.Boards.Count; i++)
            {
                var board = SourceSash.Assembly.Boards[i];
                var bvm = new BoardViewModel(board, SelectCommand) { Owner = SourceSash };

                // Сторона света
                var mid = new Core.Geometry.Point(
                    (board.StartOuter.X + board.EndOuter.X) / 2.0,
                    (board.StartOuter.Y + board.EndOuter.Y) / 2.0);
                var label = SideLabelResolver.GetLabel(mid, center);

                // DisplayName с префиксом "Створка"
                bvm.DisplayName = $"Створка {label} {bvm.BoardLength:F0}мм";

                if (i == selectedIndex)
                    bvm.IsSelected = true;

                Boards.Add(bvm);
                Children.Add(bvm);  // ← ДОБАВИТЬ (для TreeChildren)
            }
        }

        RefreshTreeChildren();
    }

    protected override void OnSelectedChanged() { }

    public override void RefreshTreeChildren()
    {
        TreeChildren = Children
            .Where(c => c is BoardViewModel or LightSpaceViewModel or JambViewModel)
            .ToList();
    }
}