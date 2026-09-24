namespace BlueprintMaker.Application;

/// <summary>
/// Единая история отмены/повтора на весь документ
/// </summary>
public sealed class DocumentHistory
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>
    /// Выполняет команду, сохраняет её в историю и очищает стек Redo
    /// </summary>
    public void Execute(IUndoableCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear(); // После нового действия старые Redo удаляются
    }

    /// <summary>
    /// Отменяет последнее действие
    /// </summary>
    public void Undo()
    {
        if (!CanUndo) return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
    }

    /// <summary>
    /// Повторяет отмененное действие
    /// </summary>
    public void Redo()
    {
        if (!CanRedo) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}