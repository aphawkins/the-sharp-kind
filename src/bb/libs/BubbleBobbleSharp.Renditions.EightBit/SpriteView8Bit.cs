// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the eight hardware sprites.</summary>
internal sealed class SpriteView8Bit : IView<SpriteModel>
{
    private const int SourceSpriteWidth = 12;
    private const int SpriteHeight = 21;

    private const float SpriteOriginX = 24;
    private const float SpriteOriginY = 50;

    private const string Sheet = "SpritesGame";

    private readonly IGraphics _graphics;
    private readonly MulticolourSprites _sprites;

    internal SpriteView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _sprites = new(surface, Sheet);
    }

    public void Draw(SpriteModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        for (int slot = SpriteModel.Capacity - 1; slot >= 0; slot--)
        {
            if (model.IsDrawn(slot))
            {
                DrawSprite(model, slot);
            }
        }
    }

    private void DrawSprite(SpriteModel model, int slot)
        => _graphics.DrawImagePart(
            _sprites.Paint(model.Colour(slot)),
            new(model.X(slot) - SpriteOriginX, model.Y(slot) - SpriteOriginY),
            new(SourceSpriteWidth * 2, SpriteHeight),
            new(model.Sprite(slot) * SourceSpriteWidth, 0),
            new(SourceSpriteWidth, SpriteHeight));
}
