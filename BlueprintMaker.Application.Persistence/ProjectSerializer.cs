using BlueprintMaker.Application.Commands;
using BlueprintMaker.Core.Geometry;
using BlueprintMaker.Domain.Models;
using BlueprintMaker.Domain.Models.Elements;
using BlueprintMaker.Domain.Models.Materialisation;
using BlueprintMaker.Domain.Models.Rules;
using System.Text.Json;
using System.Text.Json.Serialization;
// Поправьте using для Document, если он лежит в другом пространстве имён

namespace BlueprintMaker.Application.Persistence;

public sealed record LoadResult(Document Document, IReadOnlyList<string> Warnings);

public static class ProjectSerializer
{
    private const double DefaultWidth = 20;

    private static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly JsonSerializerOptions Compact = new(Pretty) { WriteIndented = false };

    // Сохранение

    public static ProjectFileDto Capture(Document doc, ViewStateDto? view = null, string? sourcePath = null)
    {
        if (doc.Root == null) throw new InvalidOperationException("Нет окна для сохранения.");
        return new ProjectFileDto
        {
            SavedUtc = DateTime.UtcNow,
            SourcePath = sourcePath,
            View = view,
            Root = CaptureNode(doc.Root)
        };
    }

    public static string ToJson(ProjectFileDto dto) => JsonSerializer.Serialize(dto, Pretty);

    /// <summary>Снимок модели без служебных полей — для сравнения «есть ли изменения».</summary>
    public static string Snapshot(Document doc) =>
        doc.Root == null ? "" : JsonSerializer.Serialize(CaptureNode(doc.Root), Compact);

    public static string SnapshotOf(NodeDto node) => JsonSerializer.Serialize(node, Compact);

    private static NodeDto CaptureNode(IWindowNode node)
    {
        var dto = new NodeDto { Kind = KindOf(node) };

        if (node is PhysicalElement pe) dto.Width = pe.Width;

        if (node is WindowNodeBase b && b.Rule == null)
            dto.Contour = ContourDto.From(b.GetContour());

        if (node is Impost imp && imp.Rule is ImpostPlacementRule r)
        {
            dto.Orientation = r.Orientation;
            dto.RelativePosition = r.RelativePosition;
        }

        if (node is Jamb j)
        {
            dto.SegmentIndex = j.SegmentIndex;
            dto.Side = j.Side;
        }

        if (node is Frame f && f.Assembly.Joints.Count > 0)
            dto.Joints = f.Assembly.Joints.Select(x => x.TypeA).ToList();

        foreach (var child in node.Children)
            dto.Children.Add(CaptureNode(child));

        return dto;
    }

    private static NodeKind KindOf(IWindowNode n) => n switch
    {
        Sash => NodeKind.Sash,          // Sash раньше Frame: он его наследник
        Frame => NodeKind.Frame,
        LightSpace => NodeKind.LightSpace,
        Impost => NodeKind.Impost,
        Jamb => NodeKind.Jamb,
        _ => throw new NotSupportedException($"Тип {n.GetType().Name} не поддерживается при сохранении.")
    };

    // Чтение

    public static ProjectFileDto FromJson(string json)
    {
        var dto = JsonSerializer.Deserialize<ProjectFileDto>(json, Pretty)
                  ?? throw new InvalidDataException("Файл пуст или повреждён.");

        if (dto.Version > ProjectFileDto.CurrentVersion)
            throw new InvalidDataException(
                $"Файл создан в более новой версии программы (формат {dto.Version}).");

        // Миграции старых форматов: while (dto.Version < Current) { ...; dto.Version++; }
        return dto;
    }

    public static LoadResult Restore(ProjectFileDto dto)
    {
        var warnings = new List<string>();
        var rootDto = dto.Root;
        if (rootDto.Kind != NodeKind.Frame || rootDto.Contour == null)
            throw new InvalidDataException("В файле нет корневой рамы.");

        var doc = new Document();
        new CreateFrameCommand(doc, rootDto.Contour.ToContour()).Execute();
        if (doc.Root == null) throw new InvalidDataException("Не удалось создать раму.");

        Apply(doc.Root, rootDto, warnings, isRoot: true);
        return new LoadResult(doc, warnings);
    }

    /// <summary>Порядок важен: форма, ширина, стыки, затем дети. Геометрия детей зависит от ширины родителя.</summary>
    private static void Apply(IWindowNode actual, NodeDto dto, List<string> w, bool isRoot = false)
    {
        try
        {
            if (!isRoot && dto.Contour != null && actual is WindowNodeBase b)
                b.RestoreFrozenContour(dto.Contour.ToContour());

            if (dto.Width > 0 && actual is PhysicalElement pe && Math.Abs(pe.Width - dto.Width) > 1e-9)
                new MaterializeCommand(pe, dto.Width).Execute();

            if (dto.Joints != null && actual is Frame f)
                RestoreJoints(f, dto.Joints, w);

            CreateChildren(actual, dto, w);
        }
        catch (Exception ex)
        {
            w.Add($"{dto.Kind}: {ex.Message}");
        }

        foreach (var (child, childDto) in MatchChildren(actual, dto, w))
            Apply(child, childDto, w);
    }

    private static void RestoreJoints(Frame f, List<JointEndType> types, List<string> w)
    {
        var joints = f.Assembly.Joints;
        if (joints.Count != types.Count)
        {
            w.Add($"Число стыков не совпало ({joints.Count} вместо {types.Count}).");
            return;
        }

        for (int i = 0; i < types.Count; i++)
            if (joints[i].TypeA != types[i])
                new ChangeJointCommand(joints[i], types[i], forA: true).Execute();
    }

    private static void CreateChildren(IWindowNode actual, NodeDto dto, List<string> w)
    {
        if (actual is LightSpace ls)
        {
            if (ls.Children.Count > 0) return;

            var imp = dto.Children.FirstOrDefault(c => c.Kind == NodeKind.Impost);
            var sash = dto.Children.FirstOrDefault(c => c.Kind == NodeKind.Sash);
            var jamb = dto.Children.FirstOrDefault(c => c.Kind == NodeKind.Jamb);

            if (imp != null)
            {
                new InsertImpostCommand(ls,
                    imp.Orientation ?? AxisOrientation.Vertical,
                    imp.RelativePosition ?? 0.5).Execute();
            }
            else if (sash != null)
            {
                new InsertSashCommand(ls, sash.Width > 0 ? sash.Width : DefaultWidth).Execute();
            }
            else if (jamb != null)
            {
                var wall = FindWall(ls);
                if (wall == null) { w.Add("Не найдена стена для добора."); return; }
                new AddJambCommand(wall, jamb.SegmentIndex ?? 0, ls,
                    jamb.Width > 0 ? jamb.Width : DefaultWidth).Execute();
            }
        }
        else if (actual is PhysicalElement pe)
        {
            foreach (var j in dto.Children.Where(c => c.Kind == NodeKind.Jamb && c.Side == JambSide.Outer))
                new AddJambCommand(pe, j.SegmentIndex ?? 0, null,
                    j.Width > 0 ? j.Width : DefaultWidth).Execute();
        }
    }

    private static List<(IWindowNode, NodeDto)> MatchChildren(IWindowNode actual, NodeDto dto, List<string> w)
    {
        var result = new List<(IWindowNode, NodeDto)>();
        var used = new HashSet<IWindowNode>();

        foreach (var cd in dto.Children)
        {
            var match = actual.Children.FirstOrDefault(c => !used.Contains(c) && Matches(c, cd));
            if (match == null)
            {
                w.Add($"Не удалось восстановить элемент «{cd.Kind}».");
                continue;
            }
            used.Add(match);
            result.Add((match, cd));
        }
        return result;
    }

    private static bool Matches(IWindowNode n, NodeDto d)
    {
        if (KindOf(n) != d.Kind) return false;
        return n is not Jamb j || (j.Side == d.Side && j.SegmentIndex == d.SegmentIndex);
    }

    private static PhysicalElement? FindWall(IWindowNode node)
    {
        for (var p = node.Parent; p != null; p = p.Parent)
            if (p is PhysicalElement pe) return pe;
        return null;
    }
}