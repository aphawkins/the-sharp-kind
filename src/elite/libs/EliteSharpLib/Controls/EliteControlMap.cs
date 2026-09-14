// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using SharpKind.Abstraction.Controls;
using SharpKind.Input;

namespace EliteSharpLib.Controls;

/// <summary>
/// The shared control map, named for Elite's own actions and axes. One name
/// rather than two type arguments repeated at every call site, and the one
/// place the flight directions are handed over.
/// </summary>
internal sealed class EliteControlMap(ControlBindings bindings, IKeyboard keyboard, IGamepad gamepad)
    : ControlMap<EliteAction, EliteAxis>(bindings, keyboard, gamepad, EliteControls.Directions);
