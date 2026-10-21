using TMPro;
using UnityEngine;

/// <summary>
/// The "LEVEL 12" pill. One prefab shared by the gameplay HUD, the main menu and the result popup,
/// so the look lives in one place. Whoever knows the level calls SetLevel.
/// </summary>
public class LevelPillView : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [Tooltip("{0} = level number. The word is drawn smaller than the number.")]
    [SerializeField] private string format = "<size=62%>LEVEL</size> {0}";

    public int Level { get; private set; }

    public void SetLevel(int level)
    {
        Level = level;
        if (label != null) label.text = Format(format, level);
    }

    public static string Format(string format, int level)
    {
        return string.Format(format, Mathf.Max(1, level));
    }
}
