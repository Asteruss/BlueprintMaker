using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Elements;
namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: импост делит пространство на 2 равные половины
/// </summary>
public sealed class SplitHalfRule : IDerivationRule
{
    private readonly Impost _impost;
    private readonly SplitSide _side;

    public SplitHalfRule(Impost impost, SplitSide side)
    {
        _impost = impost;
        _side = side;
    }

    public Contour Resolve(IWindowNode parent)
    {
        var parentContour = parent.GetContour();
        var impostContour = _impost.GetContour();

        // Режем контур родителя линией импоста
        var (path1, path2) = ContourMath.SliceContour(parentContour, impostContour);

        // Определяем, какая сторона левая, а какая правая
        var center1 = path1.BoundingBox.Center;
        var start = impostContour.Vertices[0].Position;
        var end = impostContour.Vertices[1].Position;

        // Вектор направления импоста
        double dx = end.X - start.X;
        double dy = end.Y - start.Y;

        // Вектор от начала импоста к центру path1
        double vx1 = center1.X - start.X;
        double vy1 = center1.Y - start.Y;

        // Векторное произведение (Z-компонента)
        // Если cross > 0, то path1 находится слева от направления импоста
        double cross1 = dx * vy1 - dy * vx1;

        bool path1IsLeftOrTop = cross1 > 0;

        // Если просят First (Лево/Верх), и path1 лево -> отдаем path1
        if (_side == SplitSide.First)
            return path1IsLeftOrTop ? path1 : path2;
        else
            return path1IsLeftOrTop ? path2 : path1;
    }
}


public enum SplitSide { First, Second }