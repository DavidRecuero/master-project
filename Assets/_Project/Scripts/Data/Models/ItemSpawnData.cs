/// <summary>
/// Kind of element travelling through a pipe.
/// Serialized as an int in the level JSON: 0 = Normal, 1 = Hidden, 2 = Barrel.
/// </summary>
public enum ItemType
{
    Normal = 0,
    Hidden = 1,   // Color unknown until it lands in the tray
    Barrel = 2    // Blocker with lives that holds another element inside
}

/// <summary>
/// Pure data describing one element of a pipe's queue (what to spawn, not the spawned GameObject).
/// Example JSON entries:
///   { "colorId": 2 }                                                 -> normal item
///   { "colorId": 1, "type": 1 }                                      -> hidden item
///   { "colorId": 3, "type": 2, "lives": 3, "contentHidden": true }   -> barrel holding a hidden color 3
/// </summary>
[System.Serializable]
public class ItemSpawnData
{
    public int colorId;
    public ItemType type = ItemType.Normal;

    // Barrel only: hits needed to break it
    public int lives;

    // Barrel only: when it breaks, the element inside is released as a Hidden item
    public bool contentHidden;

    public static ItemSpawnData Normal(int colorId) => new ItemSpawnData { colorId = colorId };

    public ItemSpawnData Clone() => (ItemSpawnData)MemberwiseClone();
}
