// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using System.Globalization;
using BubbleBobbleSharp.Abstractions.Views;
using SharpKind;

namespace BubbleBobbleSharp.Renditions.EightBit;

/// <summary>Draws the title screen: a logo of charset characters, and text.</summary>
internal sealed class TitleView8Bit : IView<TitleModel>
{
    private const string LogoFont = "Logo";
    private const string TextFont = "Small";

    private readonly IViewSurface _surface;

    internal TitleView8Bit(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        _surface = surface;
    }

    public void Draw(TitleModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        FastColor logo = Colour(model.LogoColour);

        for (int row = 0; row < model.LogoRows.Count; row++)
        {
            string cells = model.LogoRows[row];

            for (int column = 0; column < cells.Length; column++)
            {
                if (cells[column] != TitleModel.LogoBlank)
                {
                    _surface.Graphics.DrawTextLeft(
                        _surface.Layout.Screen.Cell(column, row),
                        cells[column].ToString(),
                        LogoFont,
                        logo);
                }
            }
        }

        foreach (TitleText line in model.Lines)
        {
            _surface.Graphics.DrawTextLeft(
                _surface.Layout.Screen.Cell(line.Column, line.Row),
                line.Text,
                TextFont,
                Colour(line.Colour));
        }
    }

    private FastColor Colour(int index) => _surface.Palette[index.ToString(CultureInfo.InvariantCulture)];
}
