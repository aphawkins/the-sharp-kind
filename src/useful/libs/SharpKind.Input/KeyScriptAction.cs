// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Input;

// Press/release within one tick, or start/end a hold spanning several. SaveFrame carries no key; it asks the host to dump the framebuffer.
public enum KeyScriptAction
{
    Tap = 0,
    Hold = 1,
    Release = 2,
    SaveFrame = 3,
}
