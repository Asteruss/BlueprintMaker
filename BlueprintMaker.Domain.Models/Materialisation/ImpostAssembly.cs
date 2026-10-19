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
    /// <summary>
    /// Пересчитывает доску импоста из central line и контура родителя.
    /// central line — осевая линия импоста (от ImpostPlacementRule).
    /// parentContour — контур LightSpace, в котором лежит импост.
    /// width — ширина бруса.
    /// </summary>
    public void Rebuild(Contour centerLine, double width, Contour? parentContour)
    {
        _width = width;

        if (width <= 0)
        {
            _board = null;
            return;
        }

        // 1. Удлиняем central line в обе стороны, чтобы концы полосы вышли за
        //    границы LightSpace. Тогда ClipPolygon обрежет по самим наклонным
        //    граням, и доска дотянется до контура с обеих сторон (трапеция
        //    вместо шестиугольника).
        var extendedCenter = ContourMath.ExtendPolyline(centerLine.Vertices, ExtendDistance);

        // 2. Полоса вокруг удлинённой central line шириной width
        var bandVerts = ContourMath.OffsetBandCenter(extendedCenter, width);

        // 3. Обрезаем по родительскому контуру (LightSpace), чтобы доска не вылезала
        if (parentContour != null)
        {
            var clippedVerts = ContourMath.ClipPolygon(bandVerts, parentContour.Vertices);
            if (clippedVerts.Count >= 3)
            {
                _board = new Board(width)
                {
                    PhysicalContour = Contour.CreateClosed(clippedVerts)
                };
                return;
            }
        }

        // Фолбэк — если обрезка не удалась или нет родителя
        _board = new Board(width)
        {
            PhysicalContour = Contour.CreateClosed(bandVerts)
        };
    }
}