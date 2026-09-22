using BlueprintMaker.Domain.Models.Elements;


namespace BlueprintMaker.Domain.Models;
/// <summary>
/// Документ. Корневой класс, содержащий окно.
/// </summary>
public sealed class Document
{
    public Frame? Root { get; private set; }

    internal void SetRoot(Frame frame) => Root = frame;
    internal void ClearRoot() => Root = null;
}