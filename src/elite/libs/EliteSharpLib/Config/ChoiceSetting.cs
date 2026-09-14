// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.UI;

namespace EliteSharpLib.Config;

/// <summary>
/// A setting whose values are a list discovered at runtime rather than an
/// enum known at compile time - the installed renditions being the one that
/// is. The stored value is the label itself, so nothing has to map an index
/// back to a name.
/// </summary>
/// <remarks>
/// The labels come through a delegate rather than a list, because some are
/// not settled when the screen is built: the attached controllers change as
/// sticks are plugged in and out while the game is running.
/// </remarks>
/// <param name="name">The label shown against the value.</param>
/// <param name="values">Reads the labels, in cycling order.</param>
/// <param name="get">Reads the current value.</param>
/// <param name="set">Stores a new value and applies whatever follows from it.</param>
internal sealed class ChoiceSetting(
    string name,
    Func<IReadOnlyList<string>> values,
    Func<string> get,
    Action<string> set) : ISetting
{
    internal ChoiceSetting(string name, IReadOnlyList<string> values, Func<string> get, Action<string> set)
        : this(name, () => values, get, set)
    {
    }

    public string Name => name;

    public IReadOnlyList<string> Values => values();

    /// <summary>
    /// Gets or sets the selected value's place in the list. The stored value
    /// is always one of them - the game would not have started otherwise -
    /// but an unknown one falls back to the first rather than leaving this
    /// screen with an index it cannot draw.
    /// </summary>
    public int SelectedIndex
    {
        get
        {
            IReadOnlyList<string> current = values();

            for (int i = 0; i < current.Count; i++)
            {
                if (string.Equals(current[i], get(), StringComparison.Ordinal))
                {
                    return i;
                }
            }

            return 0;
        }

        set
        {
            IReadOnlyList<string> current = values();

            if (value >= 0 && value < current.Count)
            {
                set(current[value]);
            }
        }
    }
}
