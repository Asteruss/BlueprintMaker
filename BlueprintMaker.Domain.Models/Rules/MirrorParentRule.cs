using BlueprintMaker.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlueprintMaker.Domain.Models.Rules;

/// <summary>
/// Стратегия: полное повторение родителя
/// Используется для створок (Sash)
/// </summary>
public sealed class MirrorParentRule : IDerivationRule
{
    public Contour Resolve(IWindowNode parent) => parent.GetContour();
    
}