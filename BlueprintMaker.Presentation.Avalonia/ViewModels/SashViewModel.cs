using Avalonia.Media;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.ObjectModel;
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
        {
            if (Boards[i].IsSelected)
            {
                selectedIndex = i;
                break;
            }
        }

        Boards.Clear();
        IsMaterialized = SourceSash.Width > 0;

        if (SourceSash.Width > 0)
        {
            SourceSash.Assembly.Rebuild(
                SourceSash.GetContour(),
                SourceSash.InnerSpace.GetContour(),
                SourceSash.Width);

            for (int i = 0; i < SourceSash.Assembly.Boards.Count; i++)
            {
                var bvm = new BoardViewModel(SourceSash.Assembly.Boards[i], SelectCommand);
                if (i == selectedIndex)
                    bvm.IsSelected = true;
                Boards.Add(bvm);
            }
        }
    }

    protected override void OnSelectedChanged() { }
}