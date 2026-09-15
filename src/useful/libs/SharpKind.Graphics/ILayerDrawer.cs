// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics;

/// <summary>
/// The drawing that goes into one <see cref="RenderLayer"/>.
/// <para>
/// An interface rather than a delegate because the objects a layer is built
/// from already have a Draw of their own. A delegate would allocate one
/// closure per layer per tick; an interface over what is already there
/// allocates nothing, and the layer list is built once.
/// </para>
/// </summary>
public interface ILayerDrawer
{
    /// <summary>
    /// Draw this layer's content. The clip region is already set to the
    /// layer's rectangle when this is called.
    /// </summary>
    public void Draw();
}
