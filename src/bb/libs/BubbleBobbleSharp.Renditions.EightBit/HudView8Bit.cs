// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>
/// Draws the eight columns beside the level: each player's label, score and
/// lives, and the high score under them.
/// <para>
/// Everything here is a charset character rather than a sprite, so it all
/// comes out of the font. The digits are screen codes $00 to $09, which is why
/// the charset runs digits first and why the export can hand them to
/// BitmapFont as ASCII; the life marker is screen code $1D, which the export
/// puts in the sheet's last cell.
/// </para>
/// <para>
/// The rows and columns below are the ones the reference writes to. Three of
/// them fall out of the pointers update_sprite_animations at $E3A7 builds, one
/// of the level's two screens being 40 columns from $5000: $50E9, $5201 and
/// $5341 are rows 5, 12 and 20, all at column 33. The other two are the
/// arguments $046C passes for the lives, $5139 and $5251, which are rows 7 and
/// 14 at the same column. The labels come from the string at $AB93, which
/// positions each one itself.
/// </para>
/// <para>
/// The colours come from that same string, which writes a colour byte to
/// colour RAM beside every character it prints: the player rows are painted as
/// it leaves them, and the digits written over them later take what is already
/// there. None of the four has bit 3 set, so unlike the playfield the HUD is
/// drawn in hires - one pixel per bit, at the font's own width.
/// </para>
/// </summary>
internal sealed class HudView8Bit : IView<HudModel>
{
    // The font the manifest declares, which is the game's own charset reordered for BitmapFont.
    private const string Font = "Small";

    // The sheet's last cell, where the export puts screen code $1D - see LIFE_ICON in
    // tools/bb/export-csharp-assets.py.
    private const char LifeMarker = '_';

    // The column every row of the HUD starts at, and the one the labels are indented to.
    private const int Column = 33;
    private const int LabelColumn = 35;

    // $AB93 writes each label a row above the score it names.
    private const int PlayerOneLabelRow = 4;
    private const int PlayerOneScoreRow = 5;
    private const int PlayerOneLivesRow = 7;
    private const int PlayerTwoLabelRow = 11;
    private const int PlayerTwoScoreRow = 12;
    private const int PlayerTwoLivesRow = 14;
    private const int HighScoreLabelRow = 18;
    private const int HighScoreRow = 20;

    private const string PlayerOneLabel = "1UP";
    private const string PlayerTwoLabel = "2UP";
    private const string HighScoreLabel = "TOP";

    // The colour bytes $AB93 leaves in colour RAM, one per band of the HUD.
    private const int LabelColour = 7;
    private const int PlayerOneColour = 5;
    private const int PlayerTwoColour = 3;
    private const int HighScoreColour = 1;

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly FastColor _label;
    private readonly FastColor _playerOne;
    private readonly FastColor _playerTwo;
    private readonly FastColor _highScore;

    internal HudView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;

        _label = Colour(surface, LabelColour);
        _playerOne = Colour(surface, PlayerOneColour);
        _playerTwo = Colour(surface, PlayerTwoColour);
        _highScore = Colour(surface, HighScoreColour);
    }

    public void Draw(HudModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        Text(LabelColumn, PlayerOneLabelRow, PlayerOneLabel, _label);
        Text(Column, PlayerOneScoreRow, model.PlayerOneScore, _playerOne);
        Lives(PlayerOneLivesRow, model.PlayerOneLives, _playerOne);

        Text(LabelColumn, PlayerTwoLabelRow, PlayerTwoLabel, _label);
        Text(Column, PlayerTwoScoreRow, model.PlayerTwoScore, _playerTwo);
        Lives(PlayerTwoLivesRow, model.PlayerTwoLives, _playerTwo);

        Text(LabelColumn, HighScoreLabelRow, HighScoreLabel, _label);
        Text(Column, HighScoreRow, model.HighScore, _highScore);
    }

    private static FastColor Colour(IViewSurface surface, int index)
        => surface.Palette[index.ToString(CultureInfo.InvariantCulture)];

    // $046C fills the row from its right-hand end, so a player with fewer lives than the row holds
    // has the empty cells at the left. A player who is out has the row left alone altogether.
    private void Lives(int row, int lives, in FastColor colour)
    {
        int markers = HudModel.LifeMarkers(lives);

        if (markers == 0)
        {
            return;
        }

        Text(
            Column + HudModel.LifeCells - markers,
            row,
            new(LifeMarker, markers),
            colour);
    }

    private void Text(int column, int row, string text, in FastColor colour)
        => _graphics.DrawTextLeft(_layout.Screen.Cell(column, row), text, Font, colour);
}
