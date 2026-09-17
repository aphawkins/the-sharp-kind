// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Rendering;

// The config's name for the IShadingModel it wants built. Enum, not a flag: the axis is which model, not whether there is one.
public enum ShadingModelKind
{
    // The flat model colour, as both games looked before there was a light to
    // turn on and as the original hardware drew them.
    Unlit = 0,

    // One directional light against each face's own normal.
    Lambert = 1,

    // A normal per corner, blended across the face, so a curve drawn as facets shades as a curve. Normals are derived, not authored - see VertexNormals.
    Gouraud = 2,
}
