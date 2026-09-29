// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Emulation.Cpu;

/// <summary>The 64 KB address space a <see cref="Cpu6502"/> reads and writes.</summary>
public interface IBus
{
    public byte Read(int address);

    public void Write(int address, byte value);
}
