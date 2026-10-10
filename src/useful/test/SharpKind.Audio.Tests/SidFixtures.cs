// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Audio.Tests;

internal static class SidFixtures
{
    // Rows of C64 Elite's effect tables (elite-source.asm, SFXPR to SFXVCH) and sounds made of them.
    internal const string EffectFile = """
        {
          "Effects": {
            "sfxplas": { "Priority": 114, "Frames": 20, "Frequency": 69, "Control": 65, "AttackDecay": 1, "SustainRelease": 209, "FrequencyChange": -2, "VolumeMask": 3 },
            "sfxwhosh": { "Priority": 115, "Frames": 63, "Frequency": 64, "Control": 129, "AttackDecay": 12, "SustainRelease": 220, "FrequencyChange": 0, "VolumeMask": 15 },
            "sfxbeep": { "Priority": 104, "Frames": 5, "Frequency": 240, "Control": 17, "AttackDecay": 0, "SustainRelease": 240, "FrequencyChange": 0, "VolumeMask": 255 },
            "sfxboop": { "Priority": 96, "Frames": 24, "Frequency": 64, "Control": 17, "AttackDecay": 99, "SustainRelease": 243, "FrequencyChange": 0, "VolumeMask": 255 },
            "sfxhyp1": { "Priority": 240, "Frames": 128, "Frequency": 128, "Control": 65, "AttackDecay": 24, "SustainRelease": 216, "FrequencyChange": 68, "VolumeMask": 31 },
            "sfxecm": { "Priority": 254, "Frames": 255, "Frequency": 80, "Control": 65, "AttackDecay": 17, "SustainRelease": 225, "FrequencyChange": 85, "VolumeMask": 255 },
            "sfxelas2": { "Priority": 2, "Frames": 14, "Frequency": 64, "Control": 33, "AttackDecay": 9, "SustainRelease": 161, "FrequencyChange": -2, "VolumeMask": 3 }
          },
          "Sounds": {
            "Pulse": [ { "Effect": "sfxplas" } ],
            "Launch": [ { "Effect": "sfxwhosh" } ],
            "Beep": [ { "Effect": "sfxbeep" } ],
            "Boop": [ { "Effect": "sfxboop" } ],
            "Ecm": [ { "Effect": "sfxecm" } ],
            "Faint": [ { "Effect": "sfxelas2" } ],
            "Hyperspace": [
              { "Effect": "sfxhyp1", "Frequency": 240, "SustainRelease": 245 },
              { "Effect": "sfxwhosh" },
              { "Effect": "sfxhyp1", "Layer": true, "DelayFrames": 1 }
            ],
            "Dock": []
          }
        }
        """;

    // A tune whose play routine sounds a saw on voice 1 for ten frames and then silences the chip; song 1 of 2.
    internal static byte[] BuildTune()
    {
        List<byte> file = [];

        void Word(int value)
        {
            file.Add((byte)(value >> 8));
            file.Add((byte)value);
        }

        file.AddRange(Encoding.ASCII.GetBytes("PSID"));
        Word(2);
        Word(0x7C);
        Word(0x1000);
        Word(0x1000);
        Word(0x1010);
        Word(2);
        Word(1);
        Word(0);
        Word(0);
        file.AddRange(new byte[96]);
        Word(0);
        Word(0);
        Word(0);

        byte[] program =
        [
            0x60,
            0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA, 0xEA,
            0xE6, 0x51,
            0xA5, 0x51,
            0xC9, 0x0A,
            0xB0, 0x1A,
            0xA9, 0x00, 0x8D, 0x00, 0xD4,
            0xA9, 0x1D, 0x8D, 0x01, 0xD4,
            0xA9, 0x00, 0x8D, 0x05, 0xD4,
            0xA9, 0xF0, 0x8D, 0x06, 0xD4,
            0xA9, 0x21, 0x8D, 0x04, 0xD4,
            0x60,
            0xA9, 0x00, 0x8D, 0x18, 0xD4,
            0xA9, 0x00, 0x8D, 0x04, 0xD4,
            0x60,
        ];

        file.AddRange(program);

        return [.. file];
    }
}
