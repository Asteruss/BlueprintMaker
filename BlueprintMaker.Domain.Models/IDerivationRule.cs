using BlueprintMaker.Core.Geometry;

namespace BlueprintMaker.Domain.Models;

/// <summary>
/// Паттерн Стратегия. Правило, по которому элемент вычисляет свою форму 
/// из формы своего родителя.
/// </summary>
public interface IDerivationRule
{
    /// <summary>
    /// Вычисляет контур элемента на основе контура родителя.
    /// </summary>
    Contour Resolve(IWindowNode parent);
}