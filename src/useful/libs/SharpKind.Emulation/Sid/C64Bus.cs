// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Cpu;

namespace SharpKind.Emulation.Sid;

/// <summary>64 KB of RAM with the SID at $D400-$D7FF (mirrored every 32 bytes). Nothing else is mapped.</summary>
public sealed class C64Bus(SidChip sid) : IBus
{
    private const int SidStart = 0xD400;
    private const int SidEnd = 0xD800;
    private const int MemorySize = 0x10000;

    private readonly SidChip _sid = sid ?? throw new ArgumentNullException(nameof(sid));
    private readonly byte[] _memory = new byte[MemorySize];

    public byte Read(int address)
    {
        int masked = address & 0xFFFF;

        return masked is >= SidStart and < SidEnd ? _sid.Read(masked - SidStart) : _memory[masked];
    }

    public void Write(int address, byte value)
    {
        int masked = address & 0xFFFF;

        if (masked is >= SidStart and < SidEnd)
        {
            _sid.Write(masked - SidStart, value);
            return;
        }

        _memory[masked] = value;
    }

    /// <summary>Zeroes the RAM.</summary>
    public void Clear() => Array.Clear(_memory);

    /// <summary>Copies bytes into RAM, ignoring the SID window.</summary>
    /// <param name="address">Where the first byte goes.</param>
    /// <param name="bytes">The bytes.</param>
    /// <exception cref="SharpKindException">The bytes run off the end of memory.</exception>
    public void Load(int address, ReadOnlySpan<byte> bytes)
    {
        if (address < 0 || address + bytes.Length > MemorySize)
        {
            throw new SharpKindException($"{bytes.Length} bytes at ${address:X4} do not fit in memory.");
        }

        bytes.CopyTo(_memory.AsSpan(address));
    }
}
