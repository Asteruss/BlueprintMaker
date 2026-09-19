namespace BlueprintMaker.Domain.Models;

/// <summary>
/// Интерфейс для элементов, которые могут быть материализованы 
/// (превращены из скелета-линии в физический брус с шириной)
/// </summary>
public interface IMaterializable
{
    /// <summary>
    /// Присваивает элементу физическую ширину бруса
    /// </summary>
    void Materialize(double width);
}