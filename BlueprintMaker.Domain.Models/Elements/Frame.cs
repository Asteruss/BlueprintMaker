using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Materialisation;

namespace BlueprintMaker.Domain.Models.Elements;

public class Frame : PhysicalElement
{
    public LightSpace InnerSpace { get; }
    public FrameAssembly Assembly { get; }

    internal Frame(Guid id, Contour contour)
        : base(id, localContour: contour, rule: null)
    {
        Assembly = new FrameAssembly();
        InnerSpace = new LightSpace(Guid.NewGuid(), this);
        AddChildInternal(InnerSpace);
    }

    protected Frame(Guid id, IDerivationRule rule)
        : base(id, localContour: null, rule: rule)
    {
        Assembly = new FrameAssembly();
        InnerSpace = new LightSpace(Guid.NewGuid(), this);
        AddChildInternal(InnerSpace);
    }

    protected override void OnMaterialized()
    {
        // При изменении ширины пересобираем доски
        Assembly.Rebuild(GetContour(), InnerSpace.GetContour(), Width);
    }

    public override IReadOnlyList<Contour> GetPhysicalGeometry()
    {
        if (Width > 0)
            return Assembly.Boards.Select(b => b.PhysicalContour).ToList();
        return base.GetPhysicalGeometry();
    }

    public void UpdateContour(Contour newContour)
    {
        UpdateLocalContour(newContour);
        Assembly.Rebuild(GetContour(), InnerSpace.GetContour(), Width);
    }



    public static Frame Create(Contour contour) => new Frame(Guid.NewGuid(), contour);

    protected override Contour RebuildContour(List<ContourVertex> vertices)
    {
        if (ContourMath.CalculateSignedArea(vertices) < 0) vertices.Reverse();
        var newContour = Contour.CreateClosed(vertices);
        if (Width > 0) Assembly.Rebuild(GetContour(), InnerSpace.GetContour(), Width);
        return newContour;
    }
}