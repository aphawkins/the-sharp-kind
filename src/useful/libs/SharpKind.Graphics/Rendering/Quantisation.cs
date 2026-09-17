// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Rendering;

// Only the method is a choice: what the display can show at all is the rendition's, a fact about the machine, not a preference.
public enum Quantisation
{
    // Take the closest colour available and accept the banding. One answer per
    // colour, so a whole face resolves at once.
    Nearest = 0,

    // Alternates per pixel between the two neighbouring colours in the proportion that averages out to the one wanted.
    Ordered = 1,
}
