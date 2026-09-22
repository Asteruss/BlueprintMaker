using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Application.Commands.Tests;

public class HistoryTests
{
    private Contour CreateRectContour(double w, double h)
    {
        var vertices = new ContourVertex[]
        {
            new(new Point(0, 0)), new(new Point(w, 0)),
            new(new Point(w, h)), new(new Point(0, h))
        };
        return Contour.CreateClosed(vertices);
    }

    [Fact]
    public void CreateFrame_Undo_RemovesFrameFromDocument()
    {
        var doc = new Document();
        var history = new DocumentHistory();
        var contour = CreateRectContour(100, 100);

        var cmd = new CreateFrameCommand(doc, contour);
        history.Execute(cmd);

        Assert.NotNull(doc.Root);
        Assert.Equal(100, doc.Root.GetContour().BoundingBox.Width, 2);

        history.Undo();
        Assert.Null(doc.Root);
    }

    [Fact]
    public void MoveNode_Undo_RestoresOldPosition()
    {
        var doc = new Document();
        var frame = Frame.Create(CreateRectContour(100, 100));
        doc.SetRoot(frame);
        var history = new DocumentHistory();

        var oldPos = frame.GetContour().Vertices[0].Position;

        history.Execute(new MoveNodeCommand(frame, 0, new Point(50, 50)));
        Assert.Equal(new Point(50, 50), frame.GetContour().Vertices[0].Position);

        history.Undo();
        Assert.Equal(oldPos, frame.GetContour().Vertices[0].Position);
    }

    [Fact]
    public void SplitSegment_Undo_RemovesCreatedNode()
    {
        var frame = Frame.Create(CreateRectContour(100, 100));
        var history = new DocumentHistory();

        Assert.Equal(4, frame.GetContour().Vertices.Count);

        history.Execute(new SplitSegmentCommand(frame, 0));
        Assert.Equal(5, frame.GetContour().Vertices.Count);

        history.Undo();
        Assert.Equal(4, frame.GetContour().Vertices.Count);
    }

    [Fact]
    public void InsertImpost_Undo_RemovesImpostAndSpaces()
    {
        var frame = Frame.Create(CreateRectContour(100, 100));
        frame.Materialize(10);
        var rootSpace = frame.InnerSpace;
        var history = new DocumentHistory();

        // Выполняем
        history.Execute(new InsertImpostCommand(rootSpace, AxisOrientation.Vertical, 0.5));

        // Проверяем, что создалось 3 ребенка (Импост + 2 пространства)
        Assert.Equal(3, rootSpace.Children.Count);

        // Откатываем
        history.Undo();

        // Пространство должно снова стать пустым листом
        Assert.Empty(rootSpace.Children);
    }
}