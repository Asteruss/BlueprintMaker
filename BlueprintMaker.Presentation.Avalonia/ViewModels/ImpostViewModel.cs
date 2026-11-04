using BlueprintMaker.Domain.Models.Elements;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public partial class ImpostViewModel : SelectableElement
{
    public Impost SourceImpost { get; }

    public ObservableCollection<ImpostSegment> Segments { get; } = new();
    public ObservableCollection<BoardViewModel> Boards { get; } = new();
    public bool IsMaterialized => SourceImpost.Width > 0;
    public ImpostViewModel(Impost impost, ReactiveCommand<SelectableElement, Unit> selectCmd) : base(selectCmd)
    {
        SourceImpost = impost;
        DisplayName = "Импост";
        UpdateState();
    }

    public void UpdateState()
    {
        Segments.Clear();
        Boards.Clear();

        if (SourceImpost.Width > 0) 
        {
            if (SourceImpost.Width > 0)
            {
                if (SourceImpost.Assembly.Board != null)
                    Boards.Add(new BoardViewModel(SourceImpost.Assembly.Board, SelectCommand) { Owner = SourceImpost });
            }
        }
        else 
        {
            var contour = SourceImpost.GetContour();
            var verts = contour.Vertices;
            for (int i = 0; i < verts.Count - 1; i++)
            {
                Segments.Add(new ImpostSegment(SourceImpost, i, verts[i], verts[i + 1], SelectCommand));
            }
        }
    }

    protected override void OnSelectedChanged() { }

    public override void RefreshTreeChildren()
    {
        TreeChildren = Children
            .Where(c => c is BoardViewModel or JambViewModel)
            .ToList();
    }
}