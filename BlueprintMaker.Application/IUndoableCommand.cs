namespace BlueprintMaker.Application;

/// <summary>
/// Паттерн Команда. Обязывает реализовать выполнение и отмену
/// </summary>
public interface IUndoableCommand
{
    void Execute();
    void Undo();
}