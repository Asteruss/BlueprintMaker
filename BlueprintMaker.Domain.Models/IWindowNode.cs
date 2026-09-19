using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models;

/// <summary>
/// Паттерн Компоновщик. Базовый контракт для любого элемента окна
/// </summary>
public interface IWindowNode
{
    /// <summary>Уникальный идентификатор элемента</summary>
    Guid Id { get; }

    /// <summary>Ссылка на родителя (у рамы верхнего уровня — null)</summary>
    IWindowNode? Parent { get; }

    /// <summary>Дочерние элементы</summary>
    IReadOnlyList<IWindowNode> Children { get; }

    /// <summary>
    /// Главный метод Pull-модели. Возвращает актуальную геометрию элемента.
    /// Если есть правило — вычисляет по нему. Если нет — возвращает локальную форму.
    /// </summary>
    Contour GetContour();
    /// <summary>
    /// Возвращает физическую ширину бруса
    /// У пустот (LightSpace) возвращает 0. У рам/импостов возвращает реальную ширину или константу DisplayWidth, если еще не материализованы
    /// </summary>
    double GetPhysicalWidth();
}