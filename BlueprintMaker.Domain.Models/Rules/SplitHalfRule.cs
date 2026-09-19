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
        var (first, second) = ContourMath.SliceContour(parentContour, impostContour);

        return _side == SplitSide.First ? first : second;
    }
}


public enum SplitSide { First, Second }