// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Numerics;
using SharpKind;
using SharpKind.Graphics.Fakes;
using StuntCarRacerSharpLib.Rendering;
using Xunit;

namespace StuntCarRacerSharpLib.Tests.Rendering;

public class HudRendererTests
{
    private const string Atlas = "Atlas";
    private const float Epsilon = 0.01f;

    [Fact]
    public void DrawsFrameWheelsAndEngineEveryFrame()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));

        // frame (top/left/right/bottom), two wheels and the engine
        Assert.True(graphics.ImageParts.Count >= 7);
        Assert.All(graphics.ImageParts, p => Assert.Equal(Atlas, p.ImageType));
    }

    [Fact]
    public void RightWheelIsMirroredAndLeftIsNot()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(2, 3, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));

        // wheel sprites are 24x56 in atlas space; a negative source width marks mirroring
        List<(string ImageType, Vector2 Position, Vector2 Size, Vector2 SourcePosition, Vector2 SourceSize)> wheelParts =
            [.. graphics.ImageParts.Where(p => IsClose(p.SourceSize.Y, 56))];
        Assert.Equal(2, wheelParts.Count);
        Assert.Single(wheelParts, p => p.SourceSize.X > 0);
        Assert.Single(wheelParts, p => p.SourceSize.X < 0);
    }

    [Fact]
    public void WheelBounceRaisesTheSpriteByTheBounceAmount()
    {
        RecordingGraphics graphics1 = new(640, 480);
        new HudRenderer(graphics1, graphics1.Layout).Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));

        RecordingGraphics graphics2 = new(640, 480);
        new HudRenderer(graphics2, graphics2.Layout).Draw(new CockpitState(0, 0, 20, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));

        float y1 = graphics1.ImageParts.First(p => IsClose(p.SourceSize.Y, 56) && p.SourceSize.X > 0).Position.Y;
        float y2 = graphics2.ImageParts.First(p => IsClose(p.SourceSize.Y, 56) && p.SourceSize.X > 0).Position.Y;

        Assert.True(y2 < y1);
    }

    [Fact]
    public void EngineFlameAnimatesOnlyWhileBoostIsActive()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));
        Vector2 noBoostSource = graphics.ImageParts.First(p => IsClose(p.SourceSize.Y, 35)).SourcePosition;

        graphics.ImageParts.Clear();
        hud.Draw(new CockpitState(0, 0, 0, 0, true, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));
        Vector2 boostSource = graphics.ImageParts.First(p => IsClose(p.SourceSize.Y, 35)).SourcePosition;

        Assert.NotEqual(noBoostSource, boostSource);
    }

    [Fact]
    public void NoDamageDrawsNoCrack()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));

        Assert.DoesNotContain(graphics.ImageParts, p => IsClose(p.SourceSize.Y, 8) && IsClose(p.SourcePosition.Y, 128));
    }

    [Theory]
    [InlineData(50)]
    [InlineData(238)]
    [InlineData(500)] // clamps to the crack's full width (238)
    public void DamageRevealsAProportionalCrackWidth(int newDamage)
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, newDamage, 0, 0, 0, 0, 0, false, false, 0, 0, null));

        (_, _, Vector2 size, _, Vector2 sourceSize) =
            graphics.ImageParts.Single(p => IsClose(p.SourcePosition.X, 0) && IsClose(p.SourcePosition.Y, 128));
        int expectedSourceWidth = Math.Min(newDamage, 238);
        Assert.Equal(expectedSourceWidth, sourceSize.X, Epsilon);

        // destination scales the same source width at a constant 2x, so it
        // looks like a reveal rather than a stretch
        Assert.Equal(expectedSourceWidth * 2, size.X, Epsilon);
    }

    [Fact]
    public void SmashHolesDrawOneImagePartEach()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 3, 0, 0, 0, 0, false, false, 0, 0, null));

        List<(string ImageType, Vector2 Position, Vector2 Size, Vector2 SourcePosition, Vector2 SourceSize)> holes =
            [.. graphics.ImageParts.Where(p => IsClose(p.SourceSize.Y, 8) && IsClose(p.SourcePosition.Y, 64))];
        Assert.Equal(3, holes.Count);

        // holes are spaced apart, not stacked
        Assert.Equal(3, holes.Select(h => h.Position.X).Distinct().Count());
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(120, 120)] // half of the 240 max
    [InlineData(240, 240)] // exactly full
    [InlineData(300, 60)] // wraps past the max, orange remainder
    public void SpeedBarWidthMatchesDisplaySpeed(int displaySpeed, int expectedUnits)
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, displaySpeed, 0, 0, 0, false, false, 0, 0, null));

        if (expectedUnits == 0)
        {
            Assert.Empty(graphics.FilledRectangles);
            return;
        }

        (_, float width, _, FastColor colour) = Assert.Single(graphics.FilledRectangles);
        float expectedWidth = expectedUnits / 240f * 242f;
        Assert.Equal(expectedWidth, width, Epsilon);
        Assert.Equal(FastColor.FromUInt32(displaySpeed > 240 ? 0xFFFFCC00 : 0xFFFFFF00), colour);
    }

    [Fact]
    public void ReadoutsShowLapBoostAndSignedDistance()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 3, 15, -340, false, false, 0, 0, null));

        Assert.Contains(graphics.LeftTexts, t => t.Text == "L3");
        Assert.Contains(graphics.LeftTexts, t => t.Text == "B15");
        Assert.Contains(graphics.LeftTexts, t => t.Text == "-0340");
    }

    [Fact]
    public void LapTimesShowCurrentAlwaysAndBestOnlyWhenSet()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 130, null));

        Assert.Contains(graphics.RightTexts, t => t.Text == "T0:02.60");
        Assert.DoesNotContain(graphics.RightTexts, t => t.Text.StartsWith('B'));

        graphics.RightTexts.Clear();
        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, 75));

        Assert.Contains(graphics.RightTexts, t => t.Text == "T0:00.00");
        Assert.Contains(graphics.RightTexts, t => t.Text == "B0:01.50");
    }

    [Fact]
    public void DrawsWithArbitraryValuesWithoutExceptions()
    {
        RecordingGraphics graphics = new(640, 480);
        HudRenderer hud = new(graphics, graphics.Layout);

        // Wheel frame is always 0-5 in practice (masked non-negative); other fields can still take unusual values.
        hud.Draw(new CockpitState(0, 0, 0, 0, false, 0, 0, 0, 0, 0, 0, false, false, 0, 0, null));
        hud.Draw(new CockpitState(5, 5, 200, 200, true, 238, 8, 0x7FFF, 4, 99, 9999, false, false, 0, 0, null));
        hud.Draw(new CockpitState(0, 5, -50, -50, false, -10, 0, -100, -1, -5, -12345, false, false, 0, 0, null));
    }

    private static bool IsClose(float value, float target) => Math.Abs(value - target) < Epsilon;
}
