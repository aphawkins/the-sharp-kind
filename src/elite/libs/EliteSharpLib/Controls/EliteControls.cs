// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Abstraction.Controls;
using SharpKind.Input;

namespace EliteSharpLib.Controls;

/// <summary>
/// Elite's half of the control bindings: which axis each flight direction
/// pushes, and the map type the game passes around.
/// </summary>
/// <remarks>
/// The shape of the file, the repair and the resolving all live in
/// <see cref="SharpKind.Abstraction.Controls"/>; what is here is the part
/// that is a fact about Elite rather than about input.
/// </remarks>
internal static class EliteControls
{
    /// <summary>
    /// Gets which way each flight direction pushes its axis, in the device's own
    /// terms: away from the pilot and to the left read negative, as SDL
    /// reports them and as the screen has it.
    /// </summary>
    /// <remarks>
    /// This is what lets a digital stick fly at all. It has no position to
    /// report, so a push past the threshold has to answer the direction
    /// action instead.
    /// </remarks>
    internal static IReadOnlyDictionary<EliteAction, (EliteAxis Axis, int Sign)> Directions { get; }
        = new Dictionary<EliteAction, (EliteAxis Axis, int Sign)>
        {
            [EliteAction.RollLeft] = (EliteAxis.Roll, -1),
            [EliteAction.RollRight] = (EliteAxis.Roll, 1),
            [EliteAction.PitchUp] = (EliteAxis.Pitch, -1),
            [EliteAction.PitchDown] = (EliteAxis.Pitch, 1),
            [EliteAction.YawLeft] = (EliteAxis.Yaw, -1),
            [EliteAction.YawRight] = (EliteAxis.Yaw, 1),
        };

    /// <summary>
    /// Builds the map Elite reads its controls through.
    /// </summary>
    /// <param name="bindings">The bindings read from the file.</param>
    /// <param name="keyboard">Where key state is read from.</param>
    /// <param name="gamepad">Where controller state is read from.</param>
    /// <returns>The map every control in the game is read through.</returns>
    internal static EliteControlMap Map(ControlBindings bindings, IKeyboard keyboard, IGamepad gamepad)
        => new(bindings, keyboard, gamepad);
}
