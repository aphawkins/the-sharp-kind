// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using SharpKind.Emulation.Cpu;
using SharpKind.Emulation.Sid;

namespace BubbleBobbleSharpLib.Tests.Oracle;

// The real game, run on SharpKind.Emulation: rebb64's `rebb64-raw.prg` loaded into 64 KB with just enough of a C64
// around it to run. It is the golden source the way VICE was, without the monitor traps: deterministic, quick, and a
// routine can be bracketed exactly (no IRQ inside it) and its memory read before and after.
//
// What is modelled: the 6510 port ($00/$01) and its I/O decode; the VIC's raster line ($D011/$D012), raster compare
// interrupt ($D019/$D01A; the read-modify-write acknowledge `dec $D019` works because the CPU writes the old value
// back first); both CIAs' timers and interrupt masks; the keyboard matrix and joystick 2 through CIA 1; and the SID.
// What is not: everything else, so no sprite collisions, no cycle-exact raster, no stolen bad-line cycles. The game
// does not need them to run.
//
// The image is found through $REBB64_PRG, else at C:\code\github\zaidka\rebb64\build\rebb64-raw.prg. Tests that need it
// skip when it is missing, so a checkout without rebb64 still passes.
internal sealed class Rebb64Machine : IBus
{
    internal const int GameEntry = 0x4830;
    internal const int Pass = 0x0A07;

    private const string DefaultImage = @"C:\code\github\zaidka\rebb64\build\rebb64-raw.prg";
    private const string ImageVariable = "REBB64_PRG";

    private const int CyclesPerLine = 63;
    private const int LinesPerFrame = 312;
    private const int IoStart = 0xD000;
    private const int IoEnd = 0xE000;

    private static readonly Lazy<byte[]?> s_image = new(ReadImage);

    private readonly byte[] _ram = new byte[0x10000];
    private readonly byte[] _vic = new byte[0x40];
    private readonly byte[][] _cia = [new byte[16], new byte[16]];
    private readonly int[] _timer = new int[4];
    private readonly int[] _latch = new int[4];
    private readonly bool[] _running = new bool[4];
    private readonly byte[] _icrFlags = new byte[2];
    private readonly byte[] _icrMask = new byte[2];
    private readonly byte[] _keys = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

    private int _instruction;
    private bool _logging;
    private int _lastLine = -1;
    private int _vicFlags;

    internal Rebb64Machine()
    {
        byte[] image = Image() ?? throw new InvalidOperationException("rebb64-raw.prg is not available.");
        int load = image[0] | (image[1] << 8);

        Array.Copy(image, 2, _ram, load, image.Length - 2);

        _ram[0] = 0x2F;
        _ram[1] = 0x37;

        for (int timer = 0; timer < _timer.Length; timer++)
        {
            _latch[timer] = 0xFFFF;
            _timer[timer] = 0xFFFF;
        }

        Cpu = new Cpu6502(this) { PC = GameEntry, S = 0xFF };
    }

    internal static bool Available => Image() is not null;

    internal Cpu6502 Cpu { get; }

    internal SidChip Sid { get; } = new();

    internal long Cycles { get; private set; }

    internal int Frame { get; private set; }

    internal int InterruptCount { get; private set; }

    // Off, the machine raises no interrupt: a bracketed routine then runs alone.
    internal bool InterruptsEnabled { get; set; } = true;

    // Joystick 2 on CIA 1 port A, as the game reads it: a bit clear is pressed. Bit 4 is fire.
    internal byte Joystick { get; set; } = 0xFF;

    internal ReadOnlySpan<byte> Ram => _ram;

    // Called for every write, with the address of the instruction that made it.
    internal Action<int, int, byte>? WriteWatch { get; set; }

    // The bytes read from CIA 1's timer B ($DC06, `CIA1_TBLO`) since the log was last cleared: the timer byte each `$E9EA` mixes in.
    internal List<byte> TimerLog { get; } = [];

    private bool IoVisible => (_ram[1] & 3) != 0 && (_ram[1] & 4) != 0;

    private int Line => (int)(Math.DivRem(Cycles, CyclesPerLine).Quotient % LinesPerFrame);

    public byte Read(int address)
    {
        int masked = address & 0xFFFF;

        return masked is >= IoStart and < IoEnd && IoVisible ? ReadIo(masked) : _ram[masked];
    }

    public void Write(int address, byte value)
    {
        int masked = address & 0xFFFF;

        WriteWatch?.Invoke(_instruction, masked, value);

        if (masked is >= IoStart and < IoEnd && IoVisible)
        {
            WriteIo(masked, value);
            return;
        }

        _ram[masked] = value;
    }

    internal byte Peek(int address) => _ram[address];

    internal int PeekWord(int address) => _ram[address] | (_ram[address + 1] << 8);

    internal void Poke(int address, byte value) => _ram[address] = value;

    internal void Poke(int address, ReadOnlySpan<byte> values) => values.CopyTo(_ram.AsSpan(address));

    // One key, or none: column and row as the keyboard matrix numbers them (the '1' key is column 7, row 0).
    internal void Press(int column, int row) => _keys[column] = (byte)~(1 << row);

    internal void ReleaseKeys() => Array.Fill(_keys, (byte)0xFF);

    internal int Step()
    {
        _instruction = Cpu.PC;

        int cycles = Cpu.Step();

        Advance(cycles);

        return cycles;
    }

    // Runs to the next time the CPU is about to execute an instruction at pc.
    internal void RunTo(int pc, long maxCycles)
    {
        long limit = Cycles + maxCycles;

        do
        {
            Step();

            if (Cycles > limit)
            {
                throw new InvalidOperationException($"${pc:X4} was not reached in {maxCycles} cycles; at ${Cpu.PC:X4}.");
            }
        }
        while (Cpu.PC != pc);
    }

    // Runs to `entry` with interrupts on, lets `setup` change the machine (an intervention, and the test must say so),
    // takes the whole of RAM, runs to `exit` with interrupts off so that nothing else can touch it, and takes it again.
    // `exit` is an address the routine returns to; the call's own JSR is at entry - 3 for a `jsr` caller.
    internal Capture Bracket(
        int entry,
        int exit,
        long maxCycles,
        Action<Rebb64Machine>? setup = null,
        Func<Rebb64Machine, bool>? when = null)
    {
        RunTo(entry, maxCycles);

        // An entry reached in a loop, for a slot other than the one wanted, is run past.
        while (when is not null && !when(this))
        {
            RunTo(entry, maxCycles);
        }

        setup?.Invoke(this);
        TimerLog.Clear();

        byte[] before = [.. _ram];
        bool enabled = InterruptsEnabled;

        InterruptsEnabled = false;
        _logging = true;
        RunTo(exit, maxCycles);
        _logging = false;
        InterruptsEnabled = enabled;

        return new Capture(before, [.. _ram], [.. TimerLog]);
    }

    // Calls a routine of the game's as `jsr` would, with interrupts off, from wherever the machine is: an intervention that
    // stands in for a call a test cannot wait for. The registers and the two stack bytes the call uses are put back.
    internal Capture Invoke(int address, byte a, byte x, long maxCycles)
    {
        (int pc, byte s, byte ra, byte rx, byte ry, byte status) = (Cpu.PC, Cpu.S, Cpu.A, Cpu.X, Cpu.Y, Cpu.Status);
        (byte low, byte high) = (_ram[0x01FE], _ram[0x01FF]);

        byte[] before = [.. _ram];
        bool enabled = InterruptsEnabled;

        InterruptsEnabled = false;
        TimerLog.Clear();
        _logging = true;
        Cpu.Call(address, a, (int)maxCycles, x, 0);
        _logging = false;
        InterruptsEnabled = enabled;

        Capture capture = new(before, [.. _ram], [.. TimerLog]);

        (Cpu.PC, Cpu.S, Cpu.A, Cpu.X, Cpu.Y, Cpu.Status) = (pc, s, ra, rx, ry, status);
        (_ram[0x01FE], _ram[0x01FF]) = (low, high);

        return capture;
    }

    // From the start of a pass: end the level as `$0A6E` does, with SUBFLG set so that the next one is `number`, and run
    // to that level's first pass. Not a way to play: the players keep whatever state they are in.
    internal void EnterLevel(int number)
    {
        Poke(0x21, 0x7F);
        Poke(0x10, (byte)(number - 2));
        Cpu.PC = 0x0A6E;
        RunTo(Pass, 985_248L * 30);
    }

    internal void RunFrames(int frames)
    {
        int target = Frame + frames;

        while (Frame < target)
        {
            Step();
        }
    }

    // The title, the credit, and the key that starts one player: to the first pass of level 1.
    internal void StartOnePlayer()
    {
        long limit = Cycles + (985_248L * 60);

        while (Cpu.PC != Pass)
        {
            Joystick = (byte)(Frame is >= 100 and < 130 ? 0xEF : 0xFF);

            if (Frame is >= 200 and < 230)
            {
                Press(7, 0);
            }
            else
            {
                ReleaseKeys();
            }

            Step();

            if (Cycles > limit)
            {
                throw new InvalidOperationException("The game did not reach its first pass.");
            }
        }

        Joystick = 0xFF;
        ReleaseKeys();
    }

    private static byte[]? Image() => s_image.Value;

    private static byte[]? ReadImage()
    {
        string path = Environment.GetEnvironmentVariable(ImageVariable) ?? DefaultImage;

        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }

    private byte ReadIo(int address) => address switch
    {
        < 0xD400 => ReadVic(address & 0x3F),
        < 0xD800 => Sid.Read(address & 0x1F),
        < 0xDC00 => _ram[address],
        < 0xDD00 => ReadCia(0, address & 0x0F),
        < 0xDE00 => ReadCia(1, address & 0x0F),
        _ => 0xFF,
    };

    private void WriteIo(int address, byte value)
    {
        switch (address)
        {
            case < 0xD400:
                WriteVic(address & 0x3F, value);
                break;
            case < 0xD800:
                Sid.Write(address & 0x1F, value);
                break;
            case < 0xDC00:
                _ram[address] = value;
                break;
            case < 0xDD00:
                WriteCia(0, address & 0x0F, value);
                break;
            case < 0xDE00:
                WriteCia(1, address & 0x0F, value);
                break;
        }
    }

    private byte ReadVic(int register) => register switch
    {
        0x11 => (byte)((_vic[0x11] & 0x7F) | ((Line & 0x100) >> 1)),
        0x12 => (byte)Line,
        0x19 => (byte)(_vicFlags | 0x70),
        0x1E or 0x1F => 0,
        _ => _vic[register],
    };

    private void WriteVic(int register, byte value)
    {
        if (register == 0x19)
        {
            _vicFlags &= ~(value & 0x0F);
            UpdateVicLine();
            return;
        }

        _vic[register] = value;

        if (register == 0x1A)
        {
            UpdateVicLine();
        }
    }

    private void UpdateVicLine() => _vicFlags = (_vicFlags & 0x0F) | (((_vicFlags & _vic[0x1A] & 0x0F) != 0) ? 0x80 : 0);

    private byte ReadCia(int chip, int register)
    {
        byte[] cia = _cia[chip];

        switch (register)
        {
            case 0x00 or 0x01 when chip == 0:
                byte selected = _cia[0][0];
                byte rows = 0xFF;

                for (int column = 0; column < 8; column++)
                {
                    if ((selected & (1 << column)) == 0)
                    {
                        rows &= _keys[column];
                    }
                }

                return register == 0 ? (byte)(Joystick & rows) : rows;
            case 0x04:
                return (byte)(_timer[chip * 2] & 0xFF);
            case 0x05:
                return (byte)(_timer[chip * 2] >> 8);
            case 0x06:
                byte low = (byte)(_timer[(chip * 2) + 1] & 0xFF);

                if (chip == 0 && _logging)
                {
                    TimerLog.Add(low);
                }

                return low;
            case 0x07:
                return (byte)(_timer[(chip * 2) + 1] >> 8);
            case 0x0D:
                byte flags = _icrFlags[chip];

                if ((flags & _icrMask[chip]) != 0)
                {
                    flags |= 0x80;
                }

                _icrFlags[chip] = 0;
                return flags;
            default:
                return cia[register];
        }
    }

    private void WriteCia(int chip, int register, byte value)
    {
        _cia[chip][register] = value;

        int a = chip * 2;
        int b = a + 1;

        switch (register)
        {
            case 0x04:
                _latch[a] = (_latch[a] & 0xFF00) | value;
                break;
            case 0x05:
                _latch[a] = (_latch[a] & 0x00FF) | (value << 8);
                _timer[a] = _running[a] ? _timer[a] : _latch[a];
                break;
            case 0x06:
                _latch[b] = (_latch[b] & 0xFF00) | value;
                break;
            case 0x07:
                _latch[b] = (_latch[b] & 0x00FF) | (value << 8);
                _timer[b] = _running[b] ? _timer[b] : _latch[b];
                break;
            case 0x0D:
                _icrMask[chip] = (value & 0x80) != 0
                    ? (byte)(_icrMask[chip] | (value & 0x1F))
                    : (byte)(_icrMask[chip] & ~(value & 0x1F));
                break;
            case 0x0E:
                _running[a] = (value & 1) != 0;
                _timer[a] = (value & 0x10) != 0 ? _latch[a] : _timer[a];
                break;
            case 0x0F:
                _running[b] = (value & 1) != 0;
                _timer[b] = (value & 0x10) != 0 ? _latch[b] : _timer[b];
                break;
        }
    }

    private void Advance(int cycles)
    {
        Cycles += cycles;

        for (int timer = 0; timer < _timer.Length; timer++)
        {
            if (_running[timer])
            {
                TickTimer(timer, cycles);
            }
        }

        int line = Line;

        if (line != _lastLine)
        {
            if (line < _lastLine)
            {
                Frame++;
            }

            _lastLine = line;

            if (line == (_vic[0x12] | ((_vic[0x11] & 0x80) << 1)))
            {
                _vicFlags |= 1;
                UpdateVicLine();
            }
        }

        bool line50 = (_vicFlags & 0x80) != 0;
        bool timerIrq = (_icrFlags[0] & _icrMask[0]) != 0;

        if (InterruptsEnabled && (line50 || timerIrq) && !Cpu.InterruptDisable)
        {
            InterruptCount++;
            Cycles += Cpu.Irq();
        }
    }

    private void TickTimer(int timer, int cycles)
    {
        _timer[timer] -= cycles;

        if (_timer[timer] >= 0)
        {
            return;
        }

        int chip = timer / 2;

        _icrFlags[chip] |= (byte)(1 << (timer % 2));

        if ((_cia[chip][0x0E + (timer % 2)] & 0x08) != 0)
        {
            _running[timer] = false;
        }

        _timer[timer] = Math.Max(_latch[timer] + _timer[timer] + 1, 0);
    }
}
