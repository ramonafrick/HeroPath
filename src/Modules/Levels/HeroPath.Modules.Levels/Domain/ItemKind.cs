namespace HeroPath.Modules.Levels.Domain;

/// <summary>Collectable item types.</summary>
public enum ItemKind
{
    /// <summary>Default item; counts towards <see cref="LevelDefinition.RequireAllItems"/>.</summary>
    Strawberry,

    /// <summary>Optional bonus item; never required.</summary>
    Pizza,
}
