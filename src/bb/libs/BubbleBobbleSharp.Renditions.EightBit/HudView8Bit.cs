// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;
using SharpKind.Graphics;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the scores, the high score and the lives.</summary>
internal sealed class HudView8Bit : IView<HudModel>
{
    private const string Font = "Small";

    private const char LifeMarker = '_';

    private const int Column = 33;
    private const int LabelColumn = 35;

    private const int PlayerOneLabelRow = 4;
    private const int PlayerOneScoreRow = 5;
    private const int PlayerOneLivesRow = 7;
    private const int PlayerTwoLabelRow = 11;
    private const int PlayerTwoScoreRow = 12;
    private const int PlayerTwoLivesRow = 14;
    private const int HighScoreLabelRow = 18;
    private const int HighScoreRow = 20;
    private const int CreditsLabelRow = 23;
    private const int CreditsDigitRow = 24;
    private const int CreditsDigitColumn = 36;

    private const string PlayerOneLabel = "1UP";
    private const string PlayerTwoLabel = "2UP";
    private const string HighScoreLabel = "TOP";
    private const string CreditsLabel = "CREDITS";
    private const string GameLabel = "GAME";
    private const string OverLabel = "OVER";

    private const int LabelColour = 7;
    private const int PlayerOneColour = 5;
    private const int PlayerTwoColour = 3;
    private const int HighScoreColour = 1;
    private const int CreditsColour = 3;
    private const int GameOverColour = 1;

    private readonly IGraphics _graphics;
    private readonly BbViewLayout _layout;
    private readonly FastColor _label;
    private readonly FastColor _playerOne;
    private readonly FastColor _playerTwo;
    private readonly FastColor _highScore;
    private readonly FastColor _credits;
    private readonly FastColor _gameOver;

    internal HudView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _graphics = surface.Graphics;
        _layout = surface.Layout;

        _label = Colour(surface, LabelColour);
        _playerOne = Colour(surface, PlayerOneColour);
        _playerTwo = Colour(surface, PlayerTwoColour);
        _highScore = Colour(surface, HighScoreColour);
        _credits = Colour(surface, CreditsColour);
        _gameOver = Colour(surface, GameOverColour);
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

        // $7B53.
        Text(Column, CreditsLabelRow, CreditsLabel, _credits);
        Text(CreditsDigitColumn, CreditsDigitRow, model.Credits.ToString(CultureInfo.InvariantCulture), _credits);
    }

    private static FastColor Colour(IViewSurface surface, int index)
        => surface.Palette[index.ToString(CultureInfo.InvariantCulture)];

    private void Lives(int row, int lives, in FastColor colour)
    {
        if (!HudModel.IsPlaying(lives))
        {
            // $7BE8, drawn over the empty row and the one below it.
            Text(LabelColumn, row, GameLabel, _gameOver);
            Text(LabelColumn, row + 1, OverLabel, _gameOver);
            return;
        }

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
