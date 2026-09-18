// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// Draws the two players: one of the game's sprites each, at the position the
/// player's own two bytes name.
/// <para>
/// A C64 sprite is 24 pixels by 21, and a multicolour one is those 24 as twelve
/// two-bit entry numbers - so a sprite is twelve pixels wide in the sheet and
/// drawn at twice that, exactly as the playfield's characters are. The sheet is
/// the game's own 97 sprites in one long row, which is how rebb64 holds them
/// and how the exporter copies them across.
/// </para>
/// <para>
/// The offset from a position byte to a pixel is the VIC-II's, because $1805
/// applies none of its own: it writes $BA and $C2 straight into the sprite's
/// registers. On a 40-by-25 display the sprite coordinate $18 puts a sprite's
/// left edge at the first pixel of the screen and $32 puts its top row at the
/// first line, so those two are what come off here.
/// </para>
/// <para>
/// This is not the offset a collision probe uses, and the two are not meant to
/// agree: <c>PlayerCell</c> works from $14 and $15, which land four pixels in
/// and five pixels down from the sprite's own corner. That is the game probing
/// a point inside the character rather than the corner of the box it is drawn
/// in, and it is why the probes that reach the ground are written as one and
/// two rows down from there.
/// </para>
/// </summary>
internal sealed class PlayerView8Bit : IView<PlayerModel>
{
    // A multicolour sprite: twelve entry numbers across in the sheet, 24 pixels on screen, 21 rows
    // tall either way.
    private const int SourceSpriteWidth = 12;
    private const int SpriteHeight = 21;

    // The VIC-II's own sprite origin for a 40-by-25 display: the coordinates that put a sprite's
    // top-left pixel at the top-left of the screen.
    private const float SpriteOriginX = 24;
    private const float SpriteOriginY = 50;

    private const string Sheet = "SpritesGame";

    private readonly IGraphics _graphics;
    private readonly MulticolourSprites _sprites;

    internal PlayerView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _sprites = new(surface, Sheet);
    }

    // $1805 walks the slots from 7 down to 0, so a lower-numbered sprite is written last. That is
    // not a drawing order on the C64 - the VIC gives sprite 0 priority over sprite 1 in hardware -
    // but drawing player two first and player one over it is the same picture.
    public void Draw(PlayerModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        for (int player = PlayerModel.Capacity - 1; player >= 0; player--)
        {
            if (model.IsActive(player))
            {
                DrawPlayer(model, player);
            }
        }
    }

    // The sheet is the 97 sprites of $5800 in one row, in the order they sit in memory, so a
    // sprite's index is its column and there is no row to work out. A player's index is masked to
    // five bits before it arrives, so it is always one of the first thirty-two.
    private void DrawPlayer(PlayerModel model, int player)
        => _graphics.DrawImagePart(
            _sprites.Paint(model.Colour(player)),
            new(model.X(player) - SpriteOriginX, model.Y(player) - SpriteOriginY),
            new(SourceSpriteWidth * 2, SpriteHeight),
            new(model.Sprite(player) * SourceSpriteWidth, 0),
            new(SourceSpriteWidth, SpriteHeight));
}
