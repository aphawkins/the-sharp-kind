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
    private const string FoodSheet = "ItemChars";

    // The food's 16 by 16 art sits in the sprite's second and third bytes, from its sixth row.
    private const float FoodOffsetX = 8;
    private const float FoodOffsetY = 5;
    private const int FoodSourceWidth = 8;
    private const int FoodHeight = 16;

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly MulticolourSprites _sprites;
    private readonly MulticolourSprites _foods;

    internal SpriteView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;
        _sprites = new(surface, Sheet);
        _foods = new(surface, FoodSheet, itemArt: true);
    }

    public void Draw(SpriteModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        for (int slot = SpriteModel.Capacity - 1; slot >= 0; slot--)
        {
            if (model.IsFood(slot))
            {
                DrawFood(model, slot);
            }
            else if (model.IsDrawn(slot))
            {
                DrawSprite(model, slot);
            }
        }
    }

    private void DrawFood(SpriteModel model, int slot)
        => _graphics.DrawImagePart(
            _foods.Paint(model.Colour(slot)),
            new(
                model.X(slot) - SpriteOriginX + FoodOffsetX,
                model.Y(slot) - SpriteOriginY + FoodOffsetY + _layout.BannerHeight),
            new(FoodSourceWidth * 2, FoodHeight),
            new(model.Food(slot) * FoodSourceWidth, 0),
            new(FoodSourceWidth, FoodHeight));

    private void DrawSprite(SpriteModel model, int slot)
        => _graphics.DrawImagePart(
            _sprites.Paint(model.Colour(slot)),
            new(model.X(slot) - SpriteOriginX, model.Y(slot) - SpriteOriginY + _layout.BannerHeight),
            new(SourceSpriteWidth * 2, SpriteHeight),
            new(model.Sprite(slot) * SourceSpriteWidth, 0),
            new(SourceSpriteWidth, SpriteHeight));
}
