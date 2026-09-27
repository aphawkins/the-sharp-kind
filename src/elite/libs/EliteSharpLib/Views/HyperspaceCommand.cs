// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Controls;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// The hyperspace key, read from every in-flight screen that honours it: the
/// cockpit windows and both charts, as in the original. One instance serves
/// them all, so a press held across a view change still fires only once.
/// </summary>
internal sealed class HyperspaceCommand
{
    private readonly GameState _gameState;
    private readonly EliteControlMap _controls;

    // Only for the Ctrl that turns hyperspace galactic; a modifier has no place in the bindings file.
    private readonly IKeyboard _keyboard;
    private readonly Space _space;

    // Gamepads report the button as a hold, so the edge is made here.
    private readonly Edge _hyperspace = new();

    internal HyperspaceCommand(GameState gameState, EliteControlMap controls, IKeyboard keyboard, Space space)
    {
        _gameState = gameState;
        _controls = controls;
        _keyboard = keyboard;
        _space = space;
    }

    internal void HandleInput()
    {
        // The stick's edge is read before the key: || short-circuiting past it would leave its state unrecorded.
        bool held = _hyperspace.Pressed(_controls.IsHeld(EliteAction.Hyperspace));
        if (!(_controls.WasPressed(EliteAction.Hyperspace) || held) || _gameState.IsDocked)
        {
            return;
        }

        // Held, not pressed: consuming Ctrl would take it from any other Ctrl combination read later this tick.
        if (_keyboard.IsHeld(ConsoleModifiers.Control))
        {
            _space.StartGalacticHyperspace();
        }
        else
        {
            _space.StartHyperspace();
        }
    }
}
