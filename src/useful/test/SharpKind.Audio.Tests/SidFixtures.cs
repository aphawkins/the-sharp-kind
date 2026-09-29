// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text;

namespace SharpKind.Audio.Tests;

internal static class SidFixtures
{
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
