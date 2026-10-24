using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using System.Collections.Generic;
using System.Linq;

namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

public sealed record JambSlot(
    PhysicalElement Wall,
    LightSpace? Space,        // null — внешний добор
    int SegmentIndex,
    Point Position,           // где рисовать стрелку
    Point Direction);         // куда растёт добор (единичный вектор)

public static class JambSlotBuilder
{
    public static IReadOnlyList<JambSlot> Build(IWindowNode? target)
    {
        var slots = new List<JambSlot>();

        if (target is Frame frame)
            AddOuter(frame, slots);
        else if (target is LightSpace space && space.Parent != null && space.Children.Count == 0)
        {
            var wall = FindWall(space);
            if (wall != null) AddInner(wall, space, slots);
        }

        return slots;
    }

    private static void AddOuter(Frame frame, List<JambSlot> slots)
    {
        var contour = frame.GetContour();
        if (!contour.IsClosed) return;

        var taken = frame.Children.OfType<Jamb>()
            .Where(j => j.Side == JambSide.Outer)
            .Select(j => j.SegmentIndex)
            .ToHashSet();

        for (int i = 0; i < contour.Vertices.Count; i++)
        {
            if (taken.Contains(i)) continue;
            var (pos, inward) = ContourMath.GetSegmentAnchor(contour, i);
            slots.Add(new JambSlot(frame, null, i, pos, new Point(-inward.X, -inward.Y)));
        }
    }

    private static void AddInner(PhysicalElement wall, LightSpace space, List<JambSlot> slots)
    {
        var contour = space.GetContour();
        for (int i = 0; i < contour.Vertices.Count; i++)
        {
            var (pos, inward) = ContourMath.GetSegmentAnchor(contour, i);
            slots.Add(new JambSlot(wall, space, i, pos, inward));
        }
    }

    private static PhysicalElement? FindWall(IWindowNode node)
    {
        for (var p = node.Parent; p != null; p = p.Parent)
            if (p is PhysicalElement pe) return pe;
        return null;
    }
}