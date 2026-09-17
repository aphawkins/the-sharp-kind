// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Planets;

// The filled planet styles. Wireframe isn't one: it comes from the engine's FillMode, which drops every planet to outlines at once.
internal enum PlanetType
{
    Solid = 0,
    Striped = 1,
    Fractal = 2,
}
