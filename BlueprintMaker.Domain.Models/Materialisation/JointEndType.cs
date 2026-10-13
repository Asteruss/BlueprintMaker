namespace BlueprintMaker.Domain.Models.Materialisation;

public enum JointEndType
{
    Miter,   // На ус (45 градусов)
    Tenon,   // Шип (доска укорачивается, отдает пространство)
    Mortise  // Паз (доска полная, доминирующая)
}