using BlueprintMaker.Core.Geometry;
using System;
using System.Collections.Generic;

namespace BlueprintMaker.Presentation.Avalonia.Helpers;

public static class SideLabelResolver
{
    private const double Threshold = 0.3;  // 30% от max — граница для уточнения

    /// <summary>
    /// Возвращает метку стороны для сегмента.
    /// </summary>
    public static string GetLabel(Point midpoint, Point center)
    {
        double dx = midpoint.X - center.X;
        double dy = midpoint.Y - center.Y;
        double absDx = Math.Abs(dx);
        double absDy = Math.Abs(dy);
        double max = Math.Max(absDx, absDy);

        if (max < 0.001) return "?";  // вырожденный

        double threshold = max * Threshold;

        // Собираем буквы: сначала основная сторона, потом уточнения
        var letters = new List<char>();

        // Лево/Право
        if (absDx > threshold)
            letters.Add(dx > 0 ? 'П' : 'Л');

        // Верх/Низ
        if (absDy > threshold)
            letters.Add(dy < 0 ? 'В' : 'Н');

        // Если ничего не добавили — сегмент близко к центру (маловероятно)
        if (letters.Count == 0)
            letters.Add(absDx >= absDy ? (dx > 0 ? 'П' : 'Л') : (dy < 0 ? 'В' : 'Н'));

        return new string(letters.ToArray());
    }

    /// <summary>
    /// Для импоста — сторона по ориентации.
    /// Vertical → "П" или "Л" (зависит от положения).
    /// Horizontal → "В" или "Н".
    /// </summary>
    public static string GetLabelForImpost(Point midpoint, Point center, AxisOrientation orientation)
    {
        double dx = midpoint.X - center.X;
        double dy = midpoint.Y - center.Y;

        if (orientation == AxisOrientation.Vertical)
            return dx > 0 ? "П" : "Л";
        else
            return dy < 0 ? "В" : "Н";
    }
}