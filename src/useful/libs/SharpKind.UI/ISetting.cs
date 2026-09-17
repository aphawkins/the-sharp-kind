// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.UI;

/// <summary>
/// One thing a <see cref="ComboBox"/> is bound to: a named choice between
/// values, and which of them is current. The control reads through this and
/// writes back through it, keeping no copy of its own, so what the screen
/// shows and what the application holds cannot drift apart.
/// <para>
/// Setting <see cref="SelectedIndex"/> is what applying a choice means: the
/// implementation is where the value is stored, where it is persisted, and
/// where anything that has to happen as a result happens. The control knows
/// none of that.
/// </para>
/// </summary>
public interface ISetting
{
    /// <summary>
    /// Gets a binding with nothing in it, for a control that shows nothing of
    /// its own - a <see cref="Container{TControl}"/> lays its children out and
    /// draws no text at all, but is still a control and so still has one.
    /// </summary>
    public static ISetting None { get; } = new TextSetting();

    /// <summary>
    /// Gets the label shown against the value.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the values this setting can take, as they are to be shown, in
    /// cycling order.
    /// </summary>
    public IReadOnlyList<string> Values { get; }

    /// <summary>
    /// Gets or sets which of <see cref="Values"/> is current. Setting it
    /// applies the choice.
    /// </summary>
    public int SelectedIndex { get; set; }

    /// <summary>
    /// Gets or sets the current value as text, which is what a control that
    /// shows or edits one value rather than choosing between several works
    /// in. Derived from the choice by default, so a setting that is a choice
    /// says nothing about it and cannot have the two disagree.
    /// <para>
    /// A setting whose value is free text - what a text box is bound to -
    /// overrides this with real storage, and leaves <see cref="Values"/>
    /// empty: there is no list to choose from.
    /// </para>
    /// </summary>
    public string Value
    {
        get => SelectedIndex >= 0 && SelectedIndex < Values.Count ? Values[SelectedIndex] : string.Empty;

        // An unknown value is not a choice this setting has, so leave it where it is rather than silently landing on the first value.
        set
        {
            for (int i = 0; i < Values.Count; i++)
            {
                if (Values[i] == value)
                {
                    SelectedIndex = i;
                    return;
                }
            }
        }
    }
}
