// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Audio;
using SharpKind.Assets;
using SharpKind.Audio;

namespace EliteSharpLib.Tests.Renditions;

/// <summary>
/// Each tier brings its own audio: SID music and C64 effects on 8-bit, MIDI and WAV on 16-bit. Every sound the game can
/// ask for has to be there in both, or that sound fails only when it is first played.
/// </summary>
[Trait("Level", "Integration")]
public class RenditionAudioTests
{
    public static TheoryData<SoundEffect> SoundEffects => [.. Enum.GetValues<SoundEffect>()];

    [Theory]
    [InlineData("8-bit")]
    [InlineData("16-bit")]
    public void EverySoundEffectResolvesToAShippedFile(string rendition)
    {
        IAssetLocator locator = TestAssets.Locator(rendition);

        Assert.All(Enum.GetNames<SoundEffect>(), name => Assert.True(File.Exists(locator.SfxPaths[name]), name));
    }

    [Theory]
    [InlineData("8-bit")]
    [InlineData("16-bit")]
    public void EveryTuneResolvesToAShippedFile(string rendition)
    {
        IAssetLocator locator = TestAssets.Locator(rendition);

        Assert.All(Enum.GetNames<MusicType>(), name => Assert.True(File.Exists(MusicPath.Parse(locator.MusicPaths[name]).File), name));
    }

    // SidSound refuses a key its .sidfx has no sound for, so building it proves every effect is in elite.sidfx.
    [Fact]
    public void Every8BitSoundEffectAndTuneIsPlayedOnTheSid()
    {
        using SidSound sid = new(TestAssets.Locator("8-bit"));

        Assert.All(Enum.GetNames<SoundEffect>(), name => Assert.True(sid.HandlesEffect(name), name));
        Assert.All(Enum.GetNames<MusicType>(), name => Assert.True(sid.HandlesMusic(name), name));
    }

    // The C64 makes no docking sound, so Dock is silent on purpose.
    [Theory]
    [MemberData(nameof(SoundEffects))]
    public void Every8BitSoundEffectSoundsOnAnIdleChip(SoundEffect effect)
    {
        using SidSound sid = new(TestAssets.Locator("8-bit"));
        float[] buffer = new float[2 * 44100 / 2];

        sid.PlayEffect(effect.ToString());
        sid.Render(buffer);

        Assert.Equal(effect != SoundEffect.Dock, buffer.Any(sample => Math.Abs(sample) > 0.01f));
    }
}
