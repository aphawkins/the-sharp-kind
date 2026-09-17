// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Input;

// One entry in a scripted input timeline, e.g. new(2, ConsoleKey.S, KeyScriptAction.Tap). SaveFrame events carry ConsoleKey.None; only the tick matters.
public readonly record struct KeyScriptEvent(
    int Tick,
    ConsoleKey Key,
    KeyScriptAction Action,
    ConsoleModifiers Modifiers = ConsoleModifiers.None);
