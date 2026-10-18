using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models.Rules;
using System.Data;

namespace BlueprintMaker.Domain.Models.Elements;



public sealed class Impost : PhysicalElement
{
    // Конструктор без ширины
    internal Impost(Guid id, IWindowNode parent, AxisOrientation orientation, double relativePosition)
        : base(id, localContour: null, rule: new ImpostPlacementRule(orientation, relativePosition))
    {
    }

    public void UpdatePosition(double newRelativePosition)
    {
        var orientation = (Rule as ImpostPlacementRule)?.Orientation ?? AxisOrientation.Vertical;
        SetRule(new ImpostPlacementRule(orientation, newRelativePosition));
    }
    public void ReprojectToParent()
    {
        // Если мы заморожены (Rule == null) и у нас есть родитель
        if (Rule == null && Parent != null)
        {
            var parentContour = Parent.GetContour();
            var currentVertices = GetContour().Vertices;

            if (currentVertices.Count < 2) return;

            // Ищем ближайшие точки на новом контуре для наших концов
            var newStart = FindClosestPointOnContour(parentContour, currentVertices[0].Position);
            var newEnd = FindClosestPointOnContour(parentContour, currentVertices[1].Position);

            var newVertices = new List<ContourVertex>
            {
                new ContourVertex(newStart, currentVertices[0].Bulge),
                new ContourVertex(newEnd, currentVertices[1].Bulge)
            };

            UpdateLocalContour(RebuildContour(newVertices));
        }
    }

    private Point FindClosestPointOnContour(Contour contour, Point target)
    {
        double minDist = double.MaxValue;
        Point bestPoint = target;

        for (int i = 0; i < contour.Vertices.Count; i++)
        {
            var from = contour.Vertices[i];
            var to = contour.Vertices[(i + 1) % contour.Vertices.Count];
            var proj = SegmentProjection.Project(from.Position, to.Position, from.Bulge, target);

            if (proj.Distance < minDist)
            {
                minDist = proj.Distance;
                bestPoint = proj.ClosestPoint;
            }
        }
        return bestPoint;
    }
    protected override Contour RebuildContour(List<ContourVertex> vertices) => Contour.CreateOpen(vertices);

}