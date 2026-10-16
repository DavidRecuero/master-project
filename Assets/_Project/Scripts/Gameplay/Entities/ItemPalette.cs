using UnityEngine;

/// <summary>
/// Visual identity of the items: for every colorId, the logical color (for effects/UI) and the finished sprite, 
/// plus the sprite used by hidden items. Designer-tunable, like BoosterDefinition.
/// The array index is the colorId used in the level JSON.
/// </summary>
[CreateAssetMenu(fileName = "ItemPalette", menuName = "Game/Item Palette")]
public class ItemPalette : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public string name;     // for readability in the inspector
        public Color color;
        public Sprite sprite;
    }

    [SerializeField] private Entry[] entries = new Entry[0];
    [SerializeField] private Sprite hiddenSprite;

    public int Count => entries != null ? entries.Length : 0;
    public Sprite HiddenSprite => hiddenSprite;

    public bool HasColor(int colorId) => entries != null && colorId >= 0 && colorId < entries.Length;

    public Color GetColor(int colorId) => HasColor(colorId) ? entries[colorId].color : Color.white;

    public Sprite GetSprite(int colorId) => HasColor(colorId) ? entries[colorId].sprite : null;

    /// <summary>Fills the palette from code (for tests and editor tools).</summary>
    public void SetData(Entry[] newEntries, Sprite newHiddenSprite)
    {
        entries = newEntries ?? new Entry[0];
        hiddenSprite = newHiddenSprite;
    }
}
