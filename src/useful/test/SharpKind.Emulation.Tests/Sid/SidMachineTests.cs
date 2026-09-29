// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;
using SharpKind.Emulation.Sid;

namespace SharpKind.Emulation.Tests.Sid;

public class SidMachineTests
{
    private const int Load = 0x1000;
    private const int Init = 0x1000;
    private const int Play = 0x1010;

    [Fact]
    public void ThePsidHeaderRoundTrips()
    {
        // Arrange
        byte[] file = BuildPsid(songs: 3, start: 2, speed: 0, load: Load, play: Play);

        // Act
        PsidFile psid = PsidFile.Read(file);

        // Assert
        Assert.Equal("Test tune", psid.Name);
        Assert.Equal("Somebody", psid.Author);
        Assert.Equal("2026", psid.Released);
        Assert.Equal(Load, psid.LoadAddress);
        Assert.Equal(Init, psid.InitAddress);
        Assert.Equal(Play, psid.PlayAddress);
        Assert.Equal(3, psid.Songs);
        Assert.Equal(2, psid.StartSong);
        Assert.Equal(0x30, psid.Data.Length);
    }

    [Fact]
    public void ALoadAddressOfZeroIsTakenFromTheData()
    {
        // Arrange
        byte[] file = BuildPsid(songs: 1, start: 1, speed: 0, load: 0, play: Play);

        // Act
        PsidFile psid = PsidFile.Read(file);

        // Assert
        Assert.Equal(0x1234, psid.LoadAddress);
        Assert.Equal(0x2E, psid.Data.Length);
    }

    [Fact]
    public void AnRsidAFileWithoutAPlayAddressAndCiaTimingAreRefused()
    {
        // Arrange
        byte[] rsid = BuildPsid(songs: 1, start: 1, speed: 0, load: Load, play: Play);
        rsid[0] = (byte)'R';

        // Act and assert
        Assert.Throws<SharpKindException>(() => PsidFile.Read(rsid));
        Assert.Throws<SharpKindException>(() => PsidFile.Read(BuildPsid(1, 1, 0, Load, 0)));
        Assert.Throws<SharpKindException>(() => PsidFile.Read(BuildPsid(1, 1, 1, Load, Play)));
    }

    [Fact]
    public void InitReceivesTheSongLessOne()
    {
        // Arrange
        SidMachine machine = new();
        PsidFile psid = PsidFile.Read(BuildPsid(songs: 3, start: 1, speed: 0, load: Load, play: Play));

        // Act
        machine.StartMusic(psid, 3);

        // Assert
        Assert.Equal(2, machine.Bus.Read(0x0050));
    }

    [Fact]
    public void ASongPastTheEndIsRefused()
    {
        // Arrange
        SidMachine machine = new();
        PsidFile psid = PsidFile.Read(BuildPsid(songs: 3, start: 1, speed: 0, load: Load, play: Play));

        // Act and assert
        Assert.Throws<SharpKindException>(() => machine.StartMusic(psid, 4));
        Assert.Throws<SharpKindException>(() => machine.StartMusic(psid, 0));
    }

    [Fact]
    public void EachFrameCallsThePlayRoutineAndItsWritesReachTheChip()
    {
        // Arrange
        SidMachine machine = new();
        machine.StartMusic(PsidFile.Read(BuildPsid(songs: 1, start: 1, speed: 0, load: Load, play: Play)), 1);
        float[] buffer = new float[2 * SidChip.SampleRate / 5];

        // Act
        machine.Render(buffer);

        // Assert: play counts frames in $51 and gates a saw on voice 1.
        int frames = machine.Bus.Read(0x0051);
        Assert.InRange(frames, 9, 11);
        Assert.Contains(buffer, sample => Math.Abs(sample) > 0.01f);
    }

    [Fact]
    public void StartingASongPutsTheVolumeBackWhateverTheLastLeft()
    {
        // Arrange
        SidMachine machine = new();
        machine.Chip.Write(0x18, 0x00);
        PsidFile psid = PsidFile.Read(BuildPsid(songs: 1, start: 1, speed: 0, load: Load, play: Play));

        // Act
        machine.StartMusic(psid, 1);
        float[] buffer = new float[2 * SidChip.SampleRate / 10];
        machine.Render(buffer);

        // Assert
        Assert.Contains(buffer, sample => Math.Abs(sample) > 0.01f);
    }

    [Fact]
    public void StopMusicSilencesTheChipAndStopsThePlayRoutine()
    {
        // Arrange
        SidMachine machine = new();
        machine.StartMusic(PsidFile.Read(BuildPsid(songs: 1, start: 1, speed: 0, load: Load, play: Play)), 1);
        machine.Render(new float[2 * SidChip.SampleRate / 10]);

        // Act
        machine.StopMusic();

        float[] after = new float[2 * SidChip.SampleRate / 2];
        machine.Render(after);
        int frames = machine.Bus.Read(0x0051);

        // Assert
        Assert.False(machine.IsPlaying);
        Assert.InRange(frames, 4, 6);
        Assert.All(after.Skip(after.Length / 2), sample => Assert.True(Math.Abs(sample) < 0.001f));
    }

    [Fact]
    public void IdleFramesRunTheHookWhenNoTuneIsPlaying()
    {
        // Arrange
        SidMachine machine = new();
        int frames = 0;
        machine.IdleFrame = () => frames++;

        // Act
        machine.Render(new float[2 * SidChip.SampleRate]);

        // Assert
        Assert.InRange(frames, 49, 51);
    }

    [Fact]
    public void TheShippedBubbleBobbleTuneRunsAndSounds()
    {
        // Arrange
        string root = FindRepositoryRoot() ?? string.Empty;
        string path = Path.Combine(
            root,
            "src",
            "bb",
            "libs",
            "BubbleBobbleSharp.Renditions.EightBit",
            "Assets",
            "Music",
            "bubble-bobble.sid");
        Assert.SkipUnless(File.Exists(path), "The tune is not in the tree.");

        PsidFile psid = PsidFile.Read(File.ReadAllBytes(path));
        SidMachine machine = new();
        machine.StartMusic(psid, psid.StartSong);
        float[] buffer = new float[2 * SidChip.SampleRate * 10];

        // Act
        machine.Render(buffer);

        // Assert
        Assert.Equal(12, psid.Songs);
        Assert.Contains(buffer, sample => Math.Abs(sample) > 0.02f);
    }

    private static string? FindRepositoryRoot()
    {
        string? directory = AppContext.BaseDirectory;

        while (directory is not null && !File.Exists(Path.Combine(directory, "TheSharpKind.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        return directory;
    }

    // A tune at $1000: init stores A in $50; play counts frames in $51 and gates a saw on voice 1 at the first frame.
    private static byte[] BuildPsid(int songs, int start, uint speed, int load, int play)
    {
        List<byte> header = [];

        void Word(int value)
        {
            header.Add((byte)(value >> 8));
            header.Add((byte)value);
        }

        void Text(string value)
        {
            byte[] bytes = new byte[32];
            Encoding.Latin1.GetBytes(value).CopyTo(bytes, 0);
            header.AddRange(bytes);
        }

        header.AddRange(Encoding.ASCII.GetBytes("PSID"));
        Word(2);
        Word(0x7C);
        Word(load);
        Word(Init);
        Word(play);
        Word(songs);
        Word(start);
        Word((int)(speed >> 16));
        Word((int)(speed & 0xFFFF));
        Text("Test tune");
        Text("Somebody");
        Text("2026");
        Word(0);
        Word(0);
        Word(0);

        byte[] program =
        [
            0x85, 0x50,             // $1000 STA $50
            0x60,                   // $1002 RTS
            0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, // padding to $1010
            0xE6, 0x51,             // $1010 INC $51
            0xA9, 0x00, 0x8D, 0x00, 0xD4,   // LDA #0, STA $D400 (voice 1 frequency low)
            0xA9, 0x1D, 0x8D, 0x01, 0xD4,   // LDA #$1D, STA $D401 (frequency high)
            0xA9, 0x00, 0x8D, 0x05, 0xD4,   // attack/decay 0
            0xA9, 0xF0, 0x8D, 0x06, 0xD4,   // sustain 15
            0xA9, 0x21, 0x8D, 0x04, 0xD4,   // saw, gate on
            0x60,
        ];

        byte[] data = new byte[0x30];
        Array.Copy(program, data, program.Length);

        // Keep the fixture's size fixed for the load address of zero variant (two address bytes go first).
        byte[] body = load == 0 ? [0x34, 0x12, .. data.Take(0x2E)] : data;

        return [.. header, .. body];
    }
}
