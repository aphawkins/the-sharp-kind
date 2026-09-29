// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Numerics;
using BubbleBobbleSharp.Abstractions.Views;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the banner across the top of the screen.</summary>
internal sealed class BannerView8Bit : IView<BannerModel>
{
    private const string Image = "Banner";

    private readonly IViewSurface _surface;

    internal BannerView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _surface = surface;
    }

    public void Draw(BannerModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.Height > 0)
        {
            Vector2 size = new(_surface.Layout.ScreenWidth, model.Height);

            _surface.Graphics.DrawImagePart(Image, Vector2.Zero, size, Vector2.Zero, size);
        }
    }
}
