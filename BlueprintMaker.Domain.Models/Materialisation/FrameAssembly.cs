using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models.Materialisation;

public sealed class FrameAssembly
{
    private readonly List<Board> _boards = new();
    private readonly List<Joint> _joints = new();

    public IReadOnlyList<Board> Boards => _boards;
    public IReadOnlyList<Joint> Joints => _joints;

    public double Width { get; private set; }

    // Перестроение сборки при изменении скелета или ширины
    public void Rebuild(Contour centerLine, double width)
    {
        _boards.Clear();
        _joints.Clear();
        Width = width;

        int n = centerLine.Vertices.Count;
        if (n < 3) return;

        // Создаем N досок (по одной на сегмент)
        for (int i = 0; i < n; i++)
        {
            var startCenter = centerLine.Vertices[i].Position;
            var endCenter = centerLine.Vertices[(i + 1) % n].Position;

            _boards.Add(new Board(startCenter, endCenter, width));
        }

        // Создаем N стыков и связываем их с досками
        for (int i = 0; i < n; i++)
        {
            var boardA = _boards[i];
            var boardB = _boards[(i + 1) % n];

            var joint = new Joint(boardA, boardB);
            _joints.Add(joint);

            boardA.EndJoint = joint;
            boardB.StartJoint = joint;
        }

        // Применяем дефолтные стыки (Ус-Ус) и пересчитываем геометрию
        foreach (var board in _boards)
        {
            board.ApplyJointTypes(JointEndType.Miter, JointEndType.Miter);
        }
    }

    // Смена типа крепления в конкретном углу рамы
    public void ChangeJoint(int jointIndex, JointEndType newTypeForA)
    {
        if (jointIndex >= 0 && jointIndex < _joints.Count)
        {
            _joints[jointIndex].SetTypeA(newTypeForA);
        }
    }
}