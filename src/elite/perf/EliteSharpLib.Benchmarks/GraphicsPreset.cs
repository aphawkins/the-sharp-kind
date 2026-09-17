// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Benchmarks;

/// <summary>
/// The graphics settings combinations these benchmarks compare.
/// </summary>
public enum GraphicsPreset
{
    /// <summary>The defaults: one colour a face, resolved before the fill.</summary>
    UnlitNearest = 0,

    /// <summary>Flat faces, dithered: sixteen possible answers a face.</summary>
    LambertOrdered = 1,

    /// <summary>Blended faces, undithered: a new colour every pixel.</summary>
    GouraudNearest = 2,

    /// <summary>Blended and dithered: the most expensive combination.</summary>
    GouraudOrdered = 3,
}

// The station is drawn through the real pipeline at a spread of camera distances, so the cost can
// be read against how much of the screen it covers. A Cobra is the control: same pipeline, far fewer faces.
