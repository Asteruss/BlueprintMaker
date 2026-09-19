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
public sealed class Frame : WindowNodeBase, IMaterializable
{
    public double Width { get; private set; }
    public LightSpace InnerSpace { get; }

    internal Frame(Guid id, Contour contour)
        : base(id, localContour: contour, rule: null)
    {
        Width = 0;

        // Автоматически создаем световое пространство с дефолтным отступом, чтобы его было видно на экране еще до материализации
        InnerSpace = new LightSpace(Guid.NewGuid(), this);
        AddChildInternal(InnerSpace);
    }

    public override double GetPhysicalWidth()
    {
        // Если скелет (ширина 0), возвращаем дефолт для UI. Иначе реальную ширину
        return Width == 0 ? GeometryConstants.DisplayWidth : Width;
    }


    public void Materialize(double frameWidth)
    {
        if (frameWidth <= 0) throw new ArgumentException("Ширина должна быть больше нуля");
        Width = frameWidth;
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