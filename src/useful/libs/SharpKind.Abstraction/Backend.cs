// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Abstraction;

// Software rasterises every frame into an off-screen bitmap and blits it through SDL; Hardware issues SDL render calls directly.
public enum Backend
{
    Software = 0,
    Hardware = 1,
}
