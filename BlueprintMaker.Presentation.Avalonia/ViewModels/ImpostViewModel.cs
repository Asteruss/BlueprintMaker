using System.Collections.ObjectModel;
using System.Reactive;
using BlueprintMaker.Domain.Models.Elements;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class ImpostViewModel : SelectableElement
{
    public Impost SourceImpost { get; }

    public ObservableCollection<ImpostSegment> Segments { get; } = new();

    public ImpostViewModel(Impost impost, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceImpost = impost;
        DisplayName = "Импост";
        UpdateSegments();
    }

    public void UpdateSegments()
    {
        Segments.Clear();
        var contour = SourceImpost.GetContour();
        var verts = contour.Vertices;

        for (int i = 0; i < verts.Count - 1; i++)
        {
            Segments.Add(new ImpostSegment(SourceImpost, i, verts[i], verts[i + 1], SelectCommand));
        }
    }

    protected override void OnSelectedChanged() { }
}