// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Audio.Sid;

namespace SharpKind.Audio.Tests;

public sealed class SidEffectFileTests
{
    private const string Beep = """
        "sfxbeep": { "Priority": 104, "Frames": 5, "Frequency": 240, "Control": 17, "AttackDecay": 0, "SustainRelease": 240, "FrequencyChange": 0, "VolumeMask": 255 }
        """;

    [Fact]
    public void EveryStepIsResolvedToItsEffect()
    {
        SidEffectFile file = SidEffectFile.Parse(SidFixtures.EffectFile, "fixture");

        SidEffectStep beep = Assert.Single(file.Sounds["Beep"]);
        Assert.Equal(new SidEffect(2, 104, 5, 240, 17, 0, 240, 0, 255), beep.Effect);
        Assert.Null(beep.Frequency);
        Assert.False(beep.Layer);

        Assert.Equal(-2, Assert.Single(file.Sounds["Pulse"]).Effect.FrequencyChange);
        Assert.Empty(file.Sounds["Dock"]);
    }

    [Fact]
    public void HyperspaceOverridesLayersAndDelays()
    {
        IReadOnlyList<SidEffectStep> steps = SidEffectFile.Parse(SidFixtures.EffectFile, "fixture").Sounds["Hyperspace"];

        Assert.Equal(3, steps.Count);
        Assert.Equal((byte?)240, steps[0].Frequency);
        Assert.Equal((byte?)245, steps[0].SustainRelease);
        Assert.Equal(1, steps[1].Effect.Index);
        Assert.True(steps[2].Layer);
        Assert.Equal(1, steps[2].DelayFrames);
        Assert.Equal(steps[0].Effect, steps[2].Effect);
    }

    [Theory]
    [InlineData(256, 0, "Priority")]
    [InlineData(-1, 0, "Priority")]
    [InlineData(1, 128, "FrequencyChange")]
    [InlineData(1, -129, "FrequencyChange")]
    public void AValueOutOfRangeThrowsSharpKindException(int priority, int frequencyChange, string expected)
    {
        string json = /*lang=json,strict*/ $$"""
            { "Effects": { "x": { "Priority": {{priority}}, "Frames": 5, "Frequency": 240, "Control": 17, "AttackDecay": 0,
              "SustainRelease": 240, "FrequencyChange": {{frequencyChange}}, "VolumeMask": 255 } }, "Sounds": {} }
            """;

        SharpKindException exception = Assert.Throws<SharpKindException>(() => SidEffectFile.Parse(json, "bad"));

        Assert.Contains(expected, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(
        /*lang=json,strict*/ """{ "Effects": {}, "Sounds": { "Beep": [ { "Effect": "sfxbeep" } ] } }""",
        "no effect 'sfxbeep'")]
    [InlineData(
        """{ "Effects": { BEEP }, "Sounds": { "Beep": [ { "Effect": "sfxbeep", "Frequency": 9 } ] } }""",
        "both")]
    [InlineData(
        """{ "Effects": { BEEP }, "Sounds": { "Beep": [ { "Effect": "sfxbeep", "DelayFrames": 256 } ] } }""",
        "DelayFrames")]
    [InlineData(
        """{ "Effects": { BEEP }, "Sounds": {}, "Extra": 1 }""",
        "not a valid")]
    [InlineData(
        /*lang=json,strict*/ """{ "Effects": { "x": { "Priority": 1 } }, "Sounds": {} }""",
        "not a valid")]
    public void AnInvalidFileThrowsSharpKindException(string json, string expected)
    {
        SharpKindException exception = Assert.Throws<SharpKindException>(
            () => SidEffectFile.Parse(json.Replace("BEEP", Beep, StringComparison.Ordinal), "bad"));

        Assert.Contains(expected, exception.Message, StringComparison.Ordinal);
    }
}
