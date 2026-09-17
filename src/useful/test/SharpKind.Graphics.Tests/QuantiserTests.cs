// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Tests;

public class QuantiserTests
{
    // The 8-bit tier's greys, evenly spaced.
    private static readonly FastColor[] s_greys =
    [
        new(0xFF000000), new(0xFF333333), new(0xFF666666),
        new(0xFF999999), new(0xFFCCCCCC), new(0xFFFFFFFF),
    ];

    [Fact]
    public void AChannelGridTakesTheNearestLevelItsDacDrives()
    {
        ChannelGridQuantiser quantiser = new(channelBits: 4);

        // 0x7A sits between the 0x77 and 0x88 levels, nearer the former.
        Assert.Equal(new FastColor(0xFF777777), quantiser.Quantise(new(0xFF7A7A7A), 0, 0));
        Assert.Equal(17f, quantiser.LevelGap);
    }

    [Fact]
    public void EightBitsAChannelIsEveryLevelThereIs()
    {
        ChannelGridQuantiser quantiser = new(channelBits: 8);

        Assert.Equal(new FastColor(0xFF7A7A7A), quantiser.Quantise(new(0xFF7A7A7A), 0, 0));
    }

    // The grid answers from a table now. It has to agree with the function
    // that built it on every channel value there is, at every tier's depth.
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(8)]
    public void AChannelGridsTableMatchesTheLevelItSnapsTo(int channelBits)
    {
        ChannelGridQuantiser quantiser = new(channelBits);

        for (int channel = 0; channel <= 255; channel++)
        {
            byte expected = channelBits >= 8
                ? (byte)channel
                : (byte)AssetColourBudget.NearestLevel(channel, (1 << channelBits) - 1);

            Assert.Equal(expected, quantiser.Quantise(new(0xFF, (byte)channel, 0, 0), 0, 0).R);
        }
    }

    [Fact]
    public void APaletteTakesTheNearestEntryItNames()
    {
        PaletteQuantiser quantiser = new(s_greys);

        Assert.Equal(new FastColor(0xFF666666), quantiser.Quantise(new(0xFF6A6A6A), 0, 0));
    }

    // Evenly spaced 0x33 apart, so that is the gap a dither has to span.
    [Fact]
    public void APalettesLevelGapIsMeasuredFromItsEntries()
        => Assert.Equal(0x33, new PaletteQuantiser(s_greys).LevelGap, 0);

    [Fact]
    public void APaletteNeedsAtLeastOneEntry()
        => Assert.Throws<ArgumentException>(() => new PaletteQuantiser([]));

    // The point of dithering: one colour between two displayable ones resolves
    // to a mix of both rather than banding to whichever is nearer.
    [Fact]
    public void ADitherSpreadsOneColourAcrossTheTwoEitherSideOfIt()
    {
        OrderedDitherQuantiser quantiser = new(new PaletteQuantiser(s_greys));
        HashSet<uint> produced = [];

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                produced.Add(quantiser.Quantise(new(0xFF4C4C4C), x, y).Argb);
            }
        }

        Assert.Equal([0xFF333333u, 0xFF666666u], produced.Order().ToHashSet());
    }

    // Halfway between two entries, the 4x4 matrix should land eight pixels on
    // each - a dither that shifted the average would darken or lighten a face.
    [Fact]
    public void ADitherKeepsTheAverageWhereTheColourWas()
    {
        OrderedDitherQuantiser quantiser = new(new PaletteQuantiser(s_greys));
        int lighter = 0;

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (quantiser.Quantise(new(0xFF4C4C4C), x, y).Argb == 0xFF666666)
                {
                    lighter++;
                }
            }
        }

        Assert.Equal(8, lighter);
    }

    [Fact]
    public void ADitherLeavesAnExactlyDisplayableColourAlone()
    {
        OrderedDitherQuantiser quantiser = new(new PaletteQuantiser(s_greys));

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                Assert.Equal(new FastColor(0xFF666666), quantiser.Quantise(new(0xFF666666), x, y));
            }
        }
    }

    // Only a dither needs the pixel; the fill reads the period to decide how
    // many answers a whole face has, and IsPositionDependent falls out of it.
    [Fact]
    public void OnlyADitherDependsOnThePixelPosition()
    {
        IColourQuantiser grid = new ChannelGridQuantiser(4);
        IColourQuantiser palette = new PaletteQuantiser(s_greys);
        IColourQuantiser dither = new OrderedDitherQuantiser(new PaletteQuantiser(s_greys));

        Assert.Equal(1, grid.Period);
        Assert.Equal(1, palette.Period);
        Assert.Equal(4, dither.Period);

        Assert.False(grid.IsPositionDependent);
        Assert.False(palette.IsPositionDependent);
        Assert.True(dither.IsPositionDependent);
    }

    // A dither's answer repeats every Period pixels on both axes, letting a flat fill resolve sixteen cells once instead of per pixel.
    [Fact]
    public void ADithersAnswerRepeatsEveryPeriodPixels()
    {
        OrderedDitherQuantiser quantiser = new(new PaletteQuantiser(s_greys));
        FastColor colour = new(0xFF5A5A5A);

        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                FastColor cell = quantiser.Quantise(colour, x, y);

                Assert.Equal(cell, quantiser.Quantise(colour, x + quantiser.Period, y));
                Assert.Equal(cell, quantiser.Quantise(colour, x, y + quantiser.Period));
            }
        }
    }
}
