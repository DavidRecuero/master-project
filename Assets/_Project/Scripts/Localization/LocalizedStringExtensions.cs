using UnityEngine.Localization;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

/// <summary>
/// Helpers to fill the local variables of a Smart String from code, e.g. {level} in "LEVEL {level}".
/// Changing a variable value refreshes the text on its own (no RefreshString needed).
/// The variable is created the first time, so the prefab does not need it set up in the Inspector.
///
/// Rule used in the project: the script that owns the LocalizedString fills its variables BEFORE
/// assigning it to a LocalizeStringEvent. A Smart String formatted without its variable logs an error.
/// </summary>
public static class LocalizedStringExtensions
{
    public static void SetVariable(this LocalizedString localizedString, string name, string value)
    {
        if (localizedString == null) return;

        if (localizedString.TryGetValue(name, out IVariable existing) && existing is StringVariable stringVariable)
        {
            stringVariable.Value = value;
            return;
        }

        localizedString.Remove(name);
        localizedString.Add(name, new StringVariable { Value = value });
    }

    public static void SetVariable(this LocalizedString localizedString, string name, int value)
    {
        if (localizedString == null) return;

        if (localizedString.TryGetValue(name, out IVariable existing) && existing is IntVariable intVariable)
        {
            intVariable.Value = value;
            return;
        }

        localizedString.Remove(name);
        localizedString.Add(name, new IntVariable { Value = value });
    }

    /// <summary>The current value of a variable (tests and logs), or null if it is not set.</summary>
    public static object GetVariableValue(this LocalizedString localizedString, string name)
    {
        if (localizedString == null || !localizedString.TryGetValue(name, out IVariable variable)) return null;
        return variable.GetSourceValue(null);
    }
}
