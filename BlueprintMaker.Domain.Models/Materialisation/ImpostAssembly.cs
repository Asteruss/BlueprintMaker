using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Materialisation;

public sealed class ImpostAssembly
{
    private const double ExtendDistance = 10000.0;
    private readonly Impost _owner;
    private Board? _board;
    public Board? Board
    {
        get
        {
            if (_owner.Width > 0)
                Rebuild(_owner.GetContour(), _owner.Width, _owner.Parent?.GetContour());
            return _board;
        }
    }

    private double _width;
    public double Width => _width;
    public ImpostAssembly(Impost owner)
    {
        _owner = owner;
    }

    public void Rebuild(Contour centerLine, double width, Contour? parentContour)
    {
        _width = width;

        if (width <= 0)
        {
            _board = null;
            return;
        }

        if (parentContour != null)
        {
            var clipped = ContourMath.ClipBandToContour(centerLine.Vertices, width, parentContour.Vertices);
            if (clipped.Count >= 3)
            {
                _board = new Board(width) { PhysicalContour = Contour.CreateClosed(clipped) };
                return;
            }
        }

        var extendedCenter = ContourMath.ExtendPolyline(centerLine.Vertices, ExtendDistance);
        var bandVerts = ContourMath.OffsetBandCenter(extendedCenter, width);
        _board = new Board(width) { PhysicalContour = Contour.CreateClosed(bandVerts) };
    }
}