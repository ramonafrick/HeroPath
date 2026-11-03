namespace HeroPath.Modules.Game.Domain;

/// <summary>
/// An insertion point in a program: the list of the repeat block <paramref name="ParentId"/> (<c>null</c> for the top level)
/// and the position <paramref name="Index"/> in that list, <c>0</c> to the list length.
/// </summary>
public readonly record struct BlockSlot(Guid? ParentId, int Index)
{
    /// <summary>A slot in the top-level list.</summary>
    public static BlockSlot Root(int index) => new(null, index);
}
