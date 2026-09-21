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

        var (path1, path2) = ContourMath.SliceContour(parentContour, impostContour);

        // Железобетонная логика: сравниваем центры габаритов получившихся кусков
        var center1 = path1.BoundingBox.Center;
        var center2 = path2.BoundingBox.Center;

        bool path1IsFirst;

        // Если импост вертикальный (разделил по горизонтали) - First должна быть левее
        if (Math.Abs(center1.X - center2.X) > 0.001)
        {
            path1IsFirst = center1.X < center2.X;
        }
        else // Импост горизонтальный - First должна быть выше
        {
            path1IsFirst = center1.Y < center2.Y;
        }

        if (_side == SplitSide.First)
            return path1IsFirst ? path1 : path2;
        else
            return path1IsFirst ? path2 : path1;
    }
}


public enum SplitSide { First, Second }