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
        return parent.GetContour();
    }
}


public enum SplitSide { First, Second }