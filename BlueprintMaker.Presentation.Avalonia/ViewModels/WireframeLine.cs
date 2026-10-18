using Avalonia.Media;
using BlueprintMaker.Core.Geometry;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class WireframeLine : ReactiveObject
{
    [Reactive] private StreamGeometry _geometry;

    public WireframeLine(ContourVertex from, ContourVertex to)
    {
        var contour = Contour.CreateOpen(new[] { from, to });
        Geometry = GeometryExtensions.ToAvaloniaGeometry(contour); 
    }
}
