using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintMaker.Application;

/// <summary>
/// Группирует несколько команд в одну атомарную операцию
/// При Undo откатывает шаги в обратном порядке
/// </summary>
public sealed class MacroCommand : IUndoableCommand
{
    private readonly List<IUndoableCommand> _steps;

    public MacroCommand(IEnumerable<IUndoableCommand> steps)
    {
        _steps = [.. steps];
    }

    public void Execute()
    {
        foreach (var step in _steps)
            step.Execute();
        
    }

    public void Undo()
    {
        // Откатываем в обратном порядке
        for (int i = _steps.Count - 1; i >= 0; i--)
            _steps[i].Undo();
        
    }
}