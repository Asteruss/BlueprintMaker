namespace BlueprintMaker.Presentation.Avalonia.ViewModels;

/// <summary>
/// Тип узла. Определяет, как UI-логика должна двигать этот узел.
/// </summary>
public enum NodeType
{
    /// <summary> Угол рамы. Двигается свободно. </summary>
    FrameCorner,

    /// <summary> Конец импоста, лежащий на раме. Двигается только по линии рамы. </summary>
    ImpostEnd,

    /// <summary> Внутренний узел импоста (точка разбиения). Двигается внутри светового пространства. </summary>
    ImpostSplit,

    /// <summary> Контрольная точка дуги сегмента</summary>
    ArcControl
}