using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

/// <summary>
/// The "LEVEL 12" pill. One prefab shared by the gameplay HUD, the main menu and the result popup,
/// so the look lives in one place. Whoever knows the level calls SetLevel.
///
/// The text is the entry common.level of the UI table, a Smart String with {level}. The word is drawn
/// smaller than the number with rich text inside the translation: "&lt;size=62%&gt;LEVEL&lt;/size&gt; {level}".
/// The pill fills {level} first and only then hands the string to the LocalizeStringEvent, so the
/// string is never formatted without its variable.
/// </summary>
public class LevelPillView : MonoBehaviour
{
    public const string LevelVariable = "level";

    [Tooltip("LocalizeStringEvent on the label. Leave its String Reference empty: the pill sets it.")]
    [SerializeField] private LocalizeStringEvent label;
    [Tooltip("UI/common.level")]
    [SerializeField] private LocalizedString levelText = new LocalizedString();

    public int Level { get; private set; }

    public void SetLevel(int level)
    {
        Level = ClampLevel(level);

        // Changing the variable refreshes the text on its own once the string is assigned
        levelText.SetVariable(LevelVariable, Level);

        if (label != null && !ReferenceEquals(label.StringReference, levelText))
            label.StringReference = levelText;
    }

    public static int ClampLevel(int level) => Mathf.Max(1, level);
}
