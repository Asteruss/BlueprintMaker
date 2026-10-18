using Avalonia.Media;
using System.Reactive;
using BlueprintMaker.Domain.Models.Elements;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class ImpostViewModel : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;
    public Impost SourceImpost { get; }

    public ImpostViewModel(Impost impost, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceImpost = impost;
        DisplayName = "Импост";
        _geometry = impost.GetContour()?.ToAvaloniaGeometry() ?? new StreamGeometry();
    }

    public IBrush StrokeColor => IsSelected ? Brushes.Blue : Brushes.Green;

    protected override void OnSelectedChanged()
    {
        base.OnSelectedChanged();
        this.RaisePropertyChanged(nameof(StrokeColor));
    }
}