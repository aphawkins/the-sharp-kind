// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Tests;

public class LambertShadingTests
{
    private static readonly Vector3 s_light = Vector3.Normalize(new(0, 0, -1));

    // Full brightness already, so the base is the colour itself.
    [Fact]
    public void AFaceTurnedFullyIntoTheLightKeepsItsColour()
    {
        FastColor shaded = LambertShading.Shade(new(255, 255, 128, 0), new(0, 0, -1), s_light, 0.25f, 255);

        Assert.Equal(new FastColor(255, 255, 128, 0), shaded);
    }

    [Fact]
    public void AFaceTurnedFullyAwayFallsToTheAmbientFloor()
    {
        FastColor shaded = LambertShading.Shade(new(255, 255, 128, 0), new(0, 0, 1), s_light, 0.25f, 255);

        Assert.Equal(new FastColor(255, 64, 32, 0), shaded);
    }

    [Fact]
    public void AlphaIsNotShaded()
    {
        FastColor shaded = LambertShading.Shade(new(128, 255, 128, 0), new(0, 0, 1), s_light, 0.25f, 255);

        Assert.Equal(128, shaded.A);
    }

    // A face whose points are collinear has no direction to light, and the
    // models do contain them.
    [Fact]
    public void ADegenerateNormalIsLeftUnshaded()
    {
        FastColor shaded = LambertShading.Shade(new(255, 255, 128, 0), Vector3.Zero, s_light, 0.25f, 255);

        Assert.Equal(new FastColor(255, 255, 128, 0), shaded);
    }

    [Fact]
    public void ANormalThatIsNotUnitLengthShadesAsItsDirection()
    {
        FastColor shaded = LambertShading.Shade(new(255, 255, 128, 0), new(0, 0, -8), s_light, 0.25f, 255);

        Assert.Equal(new FastColor(255, 255, 128, 0), shaded);
    }

    // The face is edge-on: the Lambert term is zero, the same as facing away.
    [Fact]
    public void AFaceParallelToTheLightTakesTheAmbientFloor()
    {
        FastColor shaded = LambertShading.Shade(new(255, 255, 128, 0), new(1, 0, 0), s_light, 0.25f, 255);

        Assert.Equal(new FastColor(255, 64, 32, 0), shaded);
    }

    [Fact]
    public void ShadingNeverBrightensAFaceBeyondItsUnlitBase()
    {
        FastColor baseColour = new(255, 255, 128, 0);

        for (int degrees = 0; degrees < 360; degrees += 15)
        {
            float radians = degrees * MathF.PI / 180f;
            Vector3 normal = new(MathF.Sin(radians), 0, -MathF.Cos(radians));
            FastColor shaded = LambertShading.Shade(baseColour, normal, s_light, 0.25f, 255);

            Assert.True(shaded.R <= baseColour.R);
            Assert.True(shaded.G <= baseColour.G);
            Assert.True(shaded.B <= baseColour.B);
        }
    }

    [Fact]
    public void AFullAmbientFloorLeavesEveryFaceAtItsUnlitBase()
    {
        Assert.Equal(new FastColor(255, 255, 128, 0), LambertShading.Shade(new(255, 255, 128, 0), new(0, 0, 1), s_light, 1f, 255));
        Assert.Equal(new FastColor(255, 255, 128, 0), LambertShading.Shade(new(255, 255, 128, 0), new(0, 0, -1), s_light, 1f, 255));
    }

    [Fact]
    public void AZeroAmbientFloorDarkensAFaceTurnedAwayToBlack()
    {
        FastColor shaded = LambertShading.Shade(new(255, 255, 128, 0), new(0, 0, 1), s_light, 0f, 255);

        Assert.Equal(new FastColor(255, 0, 0, 0), shaded);
    }

    // Every shade of one material has to come back to the same base, or the light shades an already-shaded model and steps survive as flicker.
    [Theory]
    [InlineData(0x555555u)] // DarkerGrey - the Coriolis station's eight main faces
    [InlineData(0x666666u)] // DarkGrey - its four next faces
    [InlineData(0x888888u)] // LightGrey - its two lightest
    [InlineData(0xEEEEEEu)] // LighterGrey
    [InlineData(0xFFFFFFu)] // White
    public void EveryGreyHasTheSameUnlitBase(uint grey)
    {
        FastColor unlit = LambertShading.UnlitBase(new(0xFF000000u | grey), 255);

        Assert.Equal(new FastColor(255, 255, 255, 255), unlit);
    }

    [Theory]
    [InlineData(0x880000u)] // Red
    [InlineData(0xCC0000u)] // LighterRed
    public void EveryShadeOfOneHueHasTheSameUnlitBase(uint red)
    {
        FastColor unlit = LambertShading.UnlitBase(new(0xFF000000u | red), 255);

        Assert.Equal(new FastColor(255, 255, 0, 0), unlit);
    }

    [Fact]
    public void TheUnlitBaseKeepsTheHuesProportions()
    {
        FastColor unlit = LambertShading.UnlitBase(new(255, 68, 68, 136), 255);

        Assert.Equal(new FastColor(255, 127, 127, 255), unlit);
    }

    // The station's docking slot is black, and black has no brightness to
    // restore nor a hue to restore it to.
    [Fact]
    public void BlackHasNoUnlitBaseToRecover()
        => Assert.Equal(new FastColor(255, 0, 0, 0), LambertShading.UnlitBase(new(255, 0, 0, 0), 255));

    // The station is painted in greys no lighter than 0x88, so a fully-lit face reaches 0xAA, not the white a flat 255 gave.
    [Fact]
    public void ALitFaceTopsOutAQuarterAboveTheModelsBrightestPaintedShade()
    {
        byte fullyLit = LambertShading.FullyLit([new(0xFF555555), new(0xFF666666), new(0xFF888888), new(0xFF000000)]);

        Assert.Equal(0xAA, fullyLit);

        FastColor intoTheLight = LambertShading.Shade(new(0xFF555555), new(0, 0, -1), s_light, 0.35f, fullyLit);

        Assert.Equal(new FastColor(255, 0xAA, 0xAA, 0xAA), intoTheLight);
    }

    // Headroom cannot run past what the display can hold.
    [Fact]
    public void TheHeadroomStopsAtFullBrightness()
        => Assert.Equal(255, LambertShading.FullyLit([new(0xFFDDDDDD), new(0xFFFFFFFF)]));

    [Fact]
    public void ABrightlyPaintedModelKeepsItsBrightness()
    {
        byte fullyLit = LambertShading.FullyLit([new(0xFFFF0000), new(0xFF880000)]);

        Assert.Equal(0xFF, fullyLit);
        Assert.Equal(
            new FastColor(255, 255, 0, 0),
            LambertShading.Shade(new(0xFF880000), new(0, 0, -1), s_light, 0.35f, fullyLit));
    }

    [Fact]
    public void AnAllBlackModelHasNoBrightnessAndStaysBlack()
    {
        byte fullyLit = LambertShading.FullyLit([new(0xFF000000), new(0xFF000000)]);

        Assert.Equal(0, fullyLit);
        Assert.Equal(
            new FastColor(255, 0, 0, 0),
            LambertShading.Shade(new(0xFF000000), new(0, 0, -1), s_light, 0.35f, fullyLit));
    }

    [Fact]
    public void ShadingCollapsesTheCoriolisGreysThenSeparatesThemByFacing()
    {
        FastColor darker = new(0xFF555555);
        FastColor lighter = new(0xFF888888);

        // Two different painted greys on faces turned the same way now agree.
        Assert.Equal(
            LambertShading.Shade(darker, new(0, 0, -1), s_light, 0.35f, 255),
            LambertShading.Shade(lighter, new(0, 0, -1), s_light, 0.35f, 255));

        // The same painted grey on faces turned differently now differs.
        Assert.NotEqual(
            LambertShading.Shade(darker, new(0, 0, -1), s_light, 0.35f, 255),
            LambertShading.Shade(darker, new(1, 0, 0), s_light, 0.35f, 255));
    }
}
