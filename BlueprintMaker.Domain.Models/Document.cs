using BlueprintMaker.Domain.Models.Elements;


namespace BlueprintMaker.Domain.Models;
/// <summary>
/// Документ. Корневой класс, содержащий окно.
/// </summary>
public sealed class Document
{
    public Frame? Root { get; private set; }

    public void SetRoot(Frame frame) => Root = frame;
    public void ClearRoot() => Root = null;
    public void Reset()
    {
        Root = null;
    }
}