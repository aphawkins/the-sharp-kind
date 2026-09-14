// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Input;

namespace SharpKind.Abstraction.Controls;

/// <summary>
/// One controller's entry in a controls file: which device it is for, and
/// what that device's axes, buttons and triggers do.
/// </summary>
/// <remarks>
/// Three kinds rather than one, because they are read differently. An axis
/// carries a position, a button carries a press, and a trigger is an axis
/// asked to behave like a button - which is how a pad reaches a control it
/// has no face button left for.
/// </remarks>
public sealed class ControllerBindings
{
    /// <summary>
    /// Gets or sets the device name this entry is for, matched against the
    /// name the driver reports. <see cref="ControlBindings.AnyDevice"/>
    /// matches anything no other entry names.
    /// </summary>
    public string Device { get; set; } = string.Empty;

    /// <summary>
    /// Gets which of the device's axes drives each flight control, by the
    /// game's name for it. Populated in place by the reader, which is why
    /// these have no setter.
    /// </summary>
    public Dictionary<string, AxisBinding> Axes { get; } = [];

    /// <inheritdoc cref="Axes"/>
    public Dictionary<string, string> Buttons { get; } = [];

    /// <inheritdoc cref="Axes"/>
    public Dictionary<string, string> Triggers { get; } = [];

    /// <inheritdoc cref="ControlBindings.Repair{TAction, TAxis}"/>
    public bool Repair<TAction, TAxis>()
        where TAction : struct, Enum
        where TAxis : struct, Enum
    {
        bool repaired = Drop(Axes, ControlBindings.IsNamed<TAxis>);
        repaired |= Drop(Buttons, ControlBindings.IsNamed<TAction>);
        repaired |= Drop(Triggers, ControlBindings.IsNamed<TAction>);

        foreach (string axis in Axes.Keys.ToList())
        {
            if (Axes[axis] is null || !Enum.TryParse<GamepadAxis>(Axes[axis].Axis, ignoreCase: true, out _))
            {
                _ = Axes.Remove(axis);
                repaired = true;
            }
        }

        repaired |= DropValues(Buttons, value => Enum.TryParse<GamepadButton>(value, ignoreCase: true, out _));

        // A trigger has to be one of the analog triggers. Naming a stick
        // axis here would make the game fire whenever it was pushed over.
        bool triggersDropped = DropValues(
            Triggers,
            value => Enum.TryParse(value, ignoreCase: true, out GamepadAxis parsed)
                && parsed is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger);

        return repaired || triggersDropped;
    }

    private static bool Drop<T>(Dictionary<string, T> bindings, Func<string, bool> valid)
    {
        bool dropped = false;

        foreach (string key in bindings.Keys.ToList())
        {
            if (!valid(key))
            {
                _ = bindings.Remove(key);
                dropped = true;
            }
        }

        return dropped;
    }

    private static bool DropValues(Dictionary<string, string> bindings, Func<string, bool> valid)
    {
        bool dropped = false;

        foreach (string key in bindings.Keys.ToList())
        {
            if (!valid(bindings[key]))
            {
                _ = bindings.Remove(key);
                dropped = true;
            }
        }

        return dropped;
    }
}
