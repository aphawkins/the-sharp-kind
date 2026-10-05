// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;

namespace BubbleBobbleSharpLib.Screens;

// $08B1-$08DA: the front end's stars, 200 characters at random cells, each $FE or $FF ($E9EA ORed with $FE). A cell
// picked twice is one star. Whatever the screen writes afterwards, a rule or text and its spaces, is written over them.
internal static class Starfield
{
    private const int Stars = 200;
    private const int Rows = 25;
    private const int Columns = 40;

    internal static TitleModel Scatter(IRandomSource random, TitleModel screen)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(screen);

        Dictionary<(int Column, int Row), bool> cells = [];

        for (int star = 0; star < Stars; star++)
        {
            int row = random.Random(Rows);
            int column = random.Random(Columns);

            cells[(column, row)] = random.TrueOrFalse();
        }

        foreach (TitleRule rule in screen.Rules)
        {
            Cover(cells, rule.Column, rule.Row, rule.Length);
        }

        foreach (TitleText line in screen.Lines)
        {
            Cover(cells, line.Column, line.Row, line.Text.Length);
        }

        return screen with { Stars = [.. cells.Select(x => new TitleStar(x.Key.Column, x.Key.Row, x.Value))] };
    }

    private static void Cover(Dictionary<(int Column, int Row), bool> cells, int column, int row, int length)
    {
        for (int cell = column; cell < column + length; cell++)
        {
            cells.Remove((cell, row));
        }
    }
}
