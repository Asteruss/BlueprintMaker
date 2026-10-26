using Avalonia.Media;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Linq;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class LightSpaceViewModel : SelectableElement
{
    [Reactive] private StreamGeometry _geometry;
    public LightSpace SourceSpace { get; }

    public LightSpaceViewModel(LightSpace space, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceSpace = space;
        DisplayName = "Свет. пространство";
        _geometry = space.GetContour()?.ToAvaloniaGeometry() ?? new StreamGeometry();
    }

    public IBrush FillColor => IsSelected ? new SolidColorBrush(Color.FromArgb(80, 0, 0, 255)) : Brushes.Transparent;

    protected override void OnSelectedChanged()
    {
        this.RaisePropertyChanged(nameof(FillColor));
    }

    public override void RefreshTreeChildren()
    {
        TreeChildren = Children
            .Where(c => c is LightSpaceViewModel or ImpostViewModel or
                            SashViewModel or BoardViewModel or JambViewModel)
            .ToList();
    }
}