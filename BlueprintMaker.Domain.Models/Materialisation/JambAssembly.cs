using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;

namespace BlueprintMaker.Domain.Models.Materialisation;

public sealed class JambAssembly
{
    private readonly Jamb _owner;
    private Board? _board;

    public JambAssembly(Jamb owner) => _owner = owner;

    public Board? Board
    {
        get
        {
            if (_owner.Width > 0) Rebuild(_owner.GetContour(), _owner.Width);
            else _board = null;
            return _board;
        }
    }

    // Контур уже обрезан по соседям родителя (как доска импоста)
    private void Rebuild(Contour strip, double width)
    {
        _board = new Board(width)
        {
            PhysicalContour = strip,
            Length = ContourMath.LongestEdgeLength(strip)
        };
    }
}