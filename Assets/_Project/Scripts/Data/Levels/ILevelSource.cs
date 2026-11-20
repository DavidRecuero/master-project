using System.Threading.Tasks;

/// <summary>Where the level files come from (Addressables in the game, a fake in the tests).</summary>
public interface ILevelSource
{
    /// <summary>Loads every level without blocking (Boot). Throws if the content can't be read.</summary>
    Task<LevelCatalog> LoadAsync();

    /// <summary>
    /// Loads every level right now, blocking (a scene played without going through the Boot). Only for local
    /// content: with remote content the Boot must have loaded it already.
    /// </summary>
    LevelCatalog LoadNow();
}
