using BlueprintMaker.Domain.Models.Rules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintMaker.Domain.Models.Elements;

/// <summary>
/// Створка. Вставляется в световое пространство.
/// Повторяет форму родителя.
/// Внутри создает новое световое пространство.
/// </summary>
public sealed class Sash : Frame
{
    internal Sash(Guid id, IWindowNode parent)
        : base(id, rule: new MirrorParentRule())
    {
    }
}