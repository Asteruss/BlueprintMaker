using BlueprintMaker.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintMaker.Domain.Models.Elements;

/// <summary>
/// Рама окна. Внешний контур. Единственный элемент, хранящий локальную геометрию напрямую.
/// </summary>
public sealed class Frame : WindowNodeBase
{
    public double Width { get; private set; }
    public LightSpace InnerSpace { get; }

    internal Frame(Guid id, Contour contour)
        : base(id, localContour: contour, rule: null)
    {
        Width = 0; // Проволока не имеет ширины

        // Автоматически создаем световое пространство с дефолтным отступом, чтобы его было видно на экране еще до материализации
        InnerSpace = new LightSpace(Guid.NewGuid(), this, 1.0);
        AddChildInternal(InnerSpace);
    }

    /// <summary>
    /// Превращает скелет в физический брус, задавая ширину
    /// </summary>
    public void Materialize(double frameWidth)
    {
        if (frameWidth <= 0) throw new ArgumentException("Ширина должна быть больше нуля.");

        Width = frameWidth;

        // Просим световое пространство обновить отступ до реальной ширины бруса
        InnerSpace.UpdateFrameWidth(frameWidth);
    }

    /// <summary>
    /// Обновление внешней формы рамы (когда пользователь тянет за углы)
    /// </summary>
    public void UpdateContour(Contour newContour)
    {
        UpdateLocalContour(newContour);
    }

    public static Frame Create(Contour contour)
    {
        return new Frame(Guid.NewGuid(), contour);
    }
}