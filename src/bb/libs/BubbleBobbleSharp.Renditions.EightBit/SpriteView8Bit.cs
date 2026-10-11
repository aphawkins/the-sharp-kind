// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Assets.Palettes;
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

    // The giant bonus: two sprites across, so 24 entries in the sheet. The block's first slot is the top left; a melon's
    // sprites hang three rows above the sheet's picture, and a diamond is the block's lower pair.
    private const int BonusSourceWidth = 24;
    private const int BonusFirstSlot = 2;
    private const int BonusLowerSlot = 4;
    private const int MelonRaise = 18;
    private const int CakeStage = 0;
    private const int MelonStage = 1;

    // The food's 16 by 16 art sits in the sprite's second and third bytes, from its sixth row.
    private const float FoodOffsetX = 8;
    private const float FoodOffsetY = 5;
    private const int FoodSourceWidth = 8;
    private const int FoodHeight = 16;

    // The giant: three sprites across and three down, in three sets of pictures (left, stunned, right).
    private const int BossSourceWidth = 36;
    private const float BossHeight = 63;
    private const int StunnedFrame = 0x09;
    private const int RightFrame = 0x18;

    // $E4DF: the dragon in a bubble is two by two sprites, two frames side by side, put at the player less ($0A, $08).
    private const string TransitSheet = "BubbleDragonInBubble";
    private const int TransitSourceWidth = 24;
    private const float TransitHeight = 42;
    private const float TransitOffsetX = 10;
    private const float TransitOffsetY = 8;
    private const int Players = 2;

    // A sprite the game builds at run time: three bytes a row, four multicolour pixels a byte. Its 01 is the first shared
    // colour, 10 the sprite's own and 11 the second shared colour.
    private const int PictureRowBytes = 3;
    private const int PixelsPerByte = 4;
    private const int MulticolourZero = 2;
    private const int MulticolourOne = 1;

    private static readonly float[] s_bonusHeights = [42, 24, 21];

    private readonly IGraphics _graphics;
    private readonly IPaletteCollection _palette;
    private readonly HashSet<string> _pictures = [];
    private readonly BbViewLayout _layout;
    private readonly MulticolourSprites _sprites;
    private readonly MulticolourSprites _foods;
    private readonly MulticolourSprites[] _bonuses;
    private readonly MulticolourSprites _boss;
    private readonly MulticolourSprites _transit;

    internal SpriteView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _palette = surface.Palette;
        _layout = surface.Layout;
        _sprites = new(surface, Sheet);
        _foods = new(surface, FoodSheet, itemArt: true);
        _boss = new(surface, "GrumpleGromit");
        _transit = new(surface, TransitSheet);
        _bonuses = [new(surface, "BonusCake"), new(surface, "BonusMelon"), new(surface, "BonusDiamond")];
    }

    public void Draw(SpriteModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.TransitFrame >= 0)
        {
            DrawTransit(model);
            return;
        }

        for (int slot = SpriteModel.Capacity - 1; slot >= 0; slot--)
        {
            if (slot == BonusFirstSlot && model.BonusStage >= 0)
            {
                DrawBonus(model);
            }

            if (slot == BonusFirstSlot && model.Boss is not null)
            {
                DrawBoss(model.Boss);
            }

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

    private static FastBitmap Paint(IReadOnlyList<byte> pictures, int first, FastColor[] colours)
    {
        FastBitmap image = new(SourceSpriteWidth, SpriteHeight);

        for (int row = 0; row < SpriteHeight; row++)
        {
            for (int column = 0; column < SourceSpriteWidth; column++)
            {
                byte bits = pictures[first + (row * PictureRowBytes) + (column / PixelsPerByte)];
                int pair = (bits >> (6 - (2 * (column % PixelsPerByte)))) & 0x03;

                image.SetPixel(column, row, colours[pair]);
            }
        }

        return image;
    }

    private void DrawTransit(SpriteModel model)
    {
        for (int slot = Players - 1; slot >= 0; slot--)
        {
            if (!model.IsDrawn(slot))
            {
                continue;
            }

            _graphics.DrawImagePart(
                _transit.Paint(model.Colour(slot)),
                new(
                    model.X(slot) - TransitOffsetX - SpriteOriginX,
                    model.Y(slot) - TransitOffsetY - SpriteOriginY + _layout.BannerHeight),
                new(TransitSourceWidth * 2, TransitHeight),
                new(model.TransitFrame * TransitSourceWidth, 0),
                new(TransitSourceWidth, TransitHeight));
        }
    }

    private void DrawBonus(SpriteModel model)
    {
        int kind = Math.Min(model.BonusStage, 2);
        int top = model.BonusStage switch
        {
            CakeStage => BonusFirstSlot,
            MelonStage => BonusFirstSlot,
            _ => BonusLowerSlot,
        };
        float raise = model.BonusStage == MelonStage ? MelonRaise : 0;

        float height = s_bonusHeights[kind];

        _graphics.DrawImagePart(
            _bonuses[kind].Paint(model.Colour(BonusFirstSlot)),
            new(model.X(BonusFirstSlot) - SpriteOriginX, model.Y(top) - SpriteOriginY + raise + _layout.BannerHeight),
            new(BonusSourceWidth * 2, height),
            new(0, 0),
            new(BonusSourceWidth, height));
    }

    private void DrawBoss(BossPicture boss)
    {
        int set = boss.Frame switch
        {
            StunnedFrame => 1,
            RightFrame => 2,
            _ => 0,
        };

        _graphics.DrawImagePart(
            _boss.Paint(boss.Colour & SpriteModel.ColourMask),
            new(boss.X - SpriteOriginX, boss.Y - SpriteOriginY + _layout.BannerHeight),
            new(BossSourceWidth * 2, BossHeight),
            new(set * BossSourceWidth, 0),
            new(BossSourceWidth, BossHeight));
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
    {
        if (model.Picture(slot) >= 0)
        {
            DrawPicture(model, slot);
            return;
        }

        _graphics.DrawImagePart(
            _sprites.Paint(model.Colour(slot)),
            new(model.X(slot) - SpriteOriginX, model.Y(slot) - SpriteOriginY + _layout.BannerHeight),
            new(SourceSpriteWidth * 2, SpriteHeight),
            new(model.Sprite(slot) * SourceSpriteWidth, 0),
            new(SourceSpriteWidth, SpriteHeight));
    }

    // $F4 onwards: a sprite the game has built, twice the size when it is expanded.
    private void DrawPicture(SpriteModel model, int slot)
    {
        int scale = model.IsExpanded(slot) ? 2 : 1;

        _graphics.DrawImagePart(
            Picture(model, model.Picture(slot), model.Colour(slot)),
            new(model.X(slot) - SpriteOriginX, model.Y(slot) - SpriteOriginY + _layout.BannerHeight),
            new(SourceSpriteWidth * 2 * scale, SpriteHeight * scale),
            new(0, 0),
            new(SourceSpriteWidth, SpriteHeight));
    }

    // The picture's bytes, as an image in one sprite colour, made the first time they are drawn in it.
    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The graphics keep the image.")]
    private string Picture(SpriteModel model, int picture, int colour)
    {
        int first = picture * SpriteModel.PictureBytes;
        HashCode hash = default;

        for (int i = 0; i < SpriteModel.PictureBytes; i++)
        {
            hash.Add(model.Pictures[first + i]);
        }

        string name = string.Create(CultureInfo.InvariantCulture, $"Picture.{colour}.{hash.ToHashCode():X8}");

        if (!_pictures.Add(name))
        {
            return name;
        }

        FastColor[] colours =
        [
            BaseColors.TransparentBlack,
            Colour(MulticolourZero),
            Colour(colour),
            Colour(MulticolourOne),
        ];

        _graphics.SetImage(name, Paint(model.Pictures, first, colours));

        return name;
    }

    private FastColor Colour(int index) => _palette[index.ToString(CultureInfo.InvariantCulture)];
}
