using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;

namespace BlueprintMaker.Application.Persistence;

public enum NodeKind { Frame, Sash, LightSpace, Impost, Jamb }

public sealed class ProjectFileDto
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime SavedUtc { get; set; }
    public string? SourcePath { get; set; }      // для файла автосохранения
    public ViewStateDto? View { get; set; }
    public NodeDto Root { get; set; } = new();
}

public sealed class ViewStateDto
{
    public double Zoom { get; set; } = 1;
    public double PanX { get; set; }
    public double PanY { get; set; }
}

public sealed class NodeDto
{
    public NodeKind Kind { get; set; }
    public double Width { get; set; }
    public ContourDto? Contour { get; set; }              // только для замороженных форм

    public AxisOrientation? Orientation { get; set; }     // импост
    public double? RelativePosition { get; set; }         // импост
    public int? SegmentIndex { get; set; }                // добор
    public JambSide? Side { get; set; }                   // добор
    public List<JointEndType>? Joints { get; set; }       // рама/створка: TypeA каждого стыка

    public List<NodeDto> Children { get; set; } = new();
}

public sealed class VertexDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Bulge { get; set; }
}

public sealed class ContourDto
{
    public bool IsClosed { get; set; }
    public List<VertexDto> Vertices { get; set; } = new();

    public static ContourDto From(Contour c) => new()
    {
        IsClosed = c.IsClosed,
        Vertices = c.Vertices
            .Select(v => new VertexDto { X = v.Position.X, Y = v.Position.Y, Bulge = v.Bulge })
            .ToList()
    };

    public Contour ToContour()
    {
        var verts = Vertices
            .Select(v => new ContourVertex(new Point(v.X, v.Y), v.Bulge))
            .ToList();
        return IsClosed ? Contour.CreateClosed(verts) : Contour.CreateOpen(verts);
    }
}