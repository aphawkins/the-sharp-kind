// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Emulation.Cpu;

/// <summary>
/// The documented instruction set of the NMOS 6502 (the 6510 in a C64), one <see cref="Step"/> at a time. Bytes wrap
/// where the chip wraps: the zero page, the stack page and the indirect jump's page. Decimal mode follows the NMOS part.
/// </summary>
public sealed partial class Cpu6502(IBus bus)
{
    private const int BreakFlag = 0x10;
    private const int UnusedFlag = 0x20;
    private const int StackBase = 0x0100;

    private const int IrqVector = 0xFFFE;

    // The return address Call pushes, so that RTS lands on $0000.
    private const int SentinelReturn = 0xFFFF;

    private readonly IBus _bus = bus ?? throw new ArgumentNullException(nameof(bus));

    public byte A { get; set; }

    public byte X { get; set; }

    public byte Y { get; set; }

    public byte S { get; set; }

    public int PC { get; set; }

    public bool Negative { get; set; }

    public bool Overflow { get; set; }

    public bool DecimalMode { get; set; }

    public bool InterruptDisable { get; set; }

    public bool Zero { get; set; }

    public bool Carry { get; set; }

    /// <summary>Gets the cycles run since construction.</summary>
    public long Cycles { get; private set; }

    /// <summary>Gets or sets the status register, as PHP would push it with the break flag clear.</summary>
    public byte Status
    {
        get => (byte)(UnusedFlag
            | (Carry ? 0x01 : 0)
            | (Zero ? 0x02 : 0)
            | (InterruptDisable ? 0x04 : 0)
            | (DecimalMode ? 0x08 : 0)
            | (Overflow ? 0x40 : 0)
            | (Negative ? 0x80 : 0));

        set
        {
            Carry = (value & 0x01) != 0;
            Zero = (value & 0x02) != 0;
            InterruptDisable = (value & 0x04) != 0;
            DecimalMode = (value & 0x08) != 0;
            Overflow = (value & 0x40) != 0;
            Negative = (value & 0x80) != 0;
        }
    }

    /// <summary>Runs a subroutine as JSR would, with X and Y zero.</summary>
    /// <param name="address">Where the subroutine starts.</param>
    /// <param name="a">The accumulator on entry.</param>
    /// <param name="maxCycles">The most cycles to allow before deciding the routine has run away.</param>
    /// <returns>The cycles taken.</returns>
    /// <exception cref="SharpKindException">The routine ran over <paramref name="maxCycles"/>.</exception>
    public int Call(int address, byte a, int maxCycles) => Call(address, a, maxCycles, 0, 0);

    /// <summary>Runs a subroutine as JSR would, and returns the cycles it took.</summary>
    /// <param name="address">Where the subroutine starts.</param>
    /// <param name="a">The accumulator on entry.</param>
    /// <param name="maxCycles">The most cycles to allow before deciding the routine has run away.</param>
    /// <param name="x">The X register on entry.</param>
    /// <param name="y">The Y register on entry.</param>
    /// <returns>The cycles taken.</returns>
    /// <exception cref="SharpKindException">The routine ran over <paramref name="maxCycles"/>.</exception>
    public int Call(int address, byte a, int maxCycles, byte x, byte y)
    {
        A = a;
        X = x;
        Y = y;
        S = 0xFF;
        Push((SentinelReturn >> 8) & 0xFF);
        Push(SentinelReturn & 0xFF);
        PC = address & 0xFFFF;

        int used = 0;

        while (PC != 0)
        {
            if (used > maxCycles)
            {
                throw new SharpKindException($"The routine at ${address:X4} ran over {maxCycles} cycles, at ${PC:X4}.");
            }

            used += Step();
        }

        return used;
    }

    /// <summary>Takes a maskable interrupt, whatever the interrupt flag says.</summary>
    /// <returns>The cycles taken.</returns>
    public int Irq()
    {
        Push(PC >> 8);
        Push(PC & 0xFF);
        Push(Status);
        InterruptDisable = true;
        PC = ReadWord(IrqVector);
        Cycles += 7;
        return 7;
    }

    /// <summary>Runs one instruction.</summary>
    /// <returns>The cycles it took.</returns>
    /// <exception cref="SharpKindException">The opcode is not one the chip documents.</exception>
    public int Step()
    {
        int start = PC;
        int code = Fetch();
        Opcode opcode = s_table[code];

        if (opcode.Operation == Operation.Invalid)
        {
            throw new SharpKindException($"Opcode ${code:X2} at ${start:X4} is not implemented.");
        }

        int cycles = opcode.Cycles;
        int address = Address(opcode.Mode, out bool crossed);

        if (crossed && opcode.PagePenalty)
        {
            cycles++;
        }

        cycles += Execute(opcode.Operation, opcode.Mode, address);
        Cycles += cycles;
        return cycles;
    }

    private static int Wrap(int value) => value & 0xFFFF;

    private static int Indexed(int origin, int index, out bool crossed)
    {
        int address = Wrap(origin + index);
        crossed = (origin & 0xFF00) != (address & 0xFF00);
        return address;
    }

    private int Address(Mode mode, out bool crossed)
    {
        crossed = false;

        switch (mode)
        {
            case Mode.Immediate:
                int immediate = PC;
                PC = Wrap(PC + 1);
                return immediate;
            case Mode.ZeroPage:
                return Fetch();
            case Mode.ZeroPageX:
                return (Fetch() + X) & 0xFF;
            case Mode.ZeroPageY:
                return (Fetch() + Y) & 0xFF;
            case Mode.Absolute:
                return FetchWord();
            case Mode.AbsoluteX:
                return Indexed(FetchWord(), X, out crossed);
            case Mode.AbsoluteY:
                return Indexed(FetchWord(), Y, out crossed);
            case Mode.IndirectX:
                return ZeroPageWord((Fetch() + X) & 0xFF);
            case Mode.IndirectY:
                return Indexed(ZeroPageWord(Fetch()), Y, out crossed);
            case Mode.Indirect:
                // The page-wrap bug: the high byte never comes from the next page.
                int pointer = FetchWord();
                return _bus.Read(pointer) | (_bus.Read((pointer & 0xFF00) | ((pointer + 1) & 0xFF)) << 8);
            case Mode.Relative:
                int offset = (sbyte)Fetch();
                return Wrap(PC + offset);
            default:
                return 0;
        }
    }

    private int ZeroPageWord(int pointer) => _bus.Read(pointer) | (_bus.Read((pointer + 1) & 0xFF) << 8);

    private int ReadWord(int address) => _bus.Read(address) | (_bus.Read(Wrap(address + 1)) << 8);

    private int Fetch()
    {
        int value = _bus.Read(PC);
        PC = Wrap(PC + 1);
        return value;
    }

    private int FetchWord()
    {
        int low = Fetch();
        return low | (Fetch() << 8);
    }

    private void Push(int value)
    {
        _bus.Write(StackBase + S, (byte)value);
        S--;
    }

    private int Pull()
    {
        S++;
        return _bus.Read(StackBase + S);
    }

    private byte SetNegativeZero(int value)
    {
        byte result = (byte)value;
        Negative = (result & 0x80) != 0;
        Zero = result == 0;
        return result;
    }

    private int Execute(Operation operation, Mode mode, int address)
    {
        switch (operation)
        {
            case Operation.Lda:
                A = SetNegativeZero(_bus.Read(address));
                break;
            case Operation.Ldx:
                X = SetNegativeZero(_bus.Read(address));
                break;
            case Operation.Ldy:
                Y = SetNegativeZero(_bus.Read(address));
                break;
            case Operation.Sta:
                _bus.Write(address, A);
                break;
            case Operation.Stx:
                _bus.Write(address, X);
                break;
            case Operation.Sty:
                _bus.Write(address, Y);
                break;
            case Operation.Tax:
                X = SetNegativeZero(A);
                break;
            case Operation.Tay:
                Y = SetNegativeZero(A);
                break;
            case Operation.Txa:
                A = SetNegativeZero(X);
                break;
            case Operation.Tya:
                A = SetNegativeZero(Y);
                break;
            case Operation.Tsx:
                X = SetNegativeZero(S);
                break;
            case Operation.Txs:
                S = X;
                break;
            case Operation.Nop:
                break;
            case Operation.Asl or Operation.Lsr or Operation.Rol or Operation.Ror:
                Shift(operation, mode, address);
                break;
            case Operation.Adc or Operation.Sbc or Operation.And or Operation.Ora or Operation.Eor
                or Operation.Cmp or Operation.Cpx or Operation.Cpy or Operation.Bit:
                Compute(operation, address);
                break;
            case Operation.Inc or Operation.Dec or Operation.Inx or Operation.Iny or Operation.Dex or Operation.Dey:
                Increment(operation, address);
                break;
            case Operation.Bcc or Operation.Bcs or Operation.Beq or Operation.Bmi
                or Operation.Bne or Operation.Bpl or Operation.Bvc or Operation.Bvs:
                return Branch(operation, address);
            default:
                Control(operation, address);
                break;
        }

        return 0;
    }

    private void Compute(Operation operation, int address)
    {
        switch (operation)
        {
            case Operation.Adc:
                Add(_bus.Read(address));
                break;
            case Operation.Sbc:
                Subtract(_bus.Read(address));
                break;
            case Operation.And:
                A = SetNegativeZero(A & _bus.Read(address));
                break;
            case Operation.Ora:
                A = SetNegativeZero(A | _bus.Read(address));
                break;
            case Operation.Eor:
                A = SetNegativeZero(A ^ _bus.Read(address));
                break;
            case Operation.Cmp:
                Compare(A, _bus.Read(address));
                break;
            case Operation.Cpx:
                Compare(X, _bus.Read(address));
                break;
            case Operation.Cpy:
                Compare(Y, _bus.Read(address));
                break;
            default:
                TestBits(address);
                break;
        }
    }

    private void TestBits(int address)
    {
        int operand = _bus.Read(address);
        Zero = (A & operand) == 0;
        Negative = (operand & 0x80) != 0;
        Overflow = (operand & 0x40) != 0;
    }

    private void Increment(Operation operation, int address)
    {
        switch (operation)
        {
            case Operation.Inc:
                _bus.Write(address, SetNegativeZero(_bus.Read(address) + 1));
                break;
            case Operation.Dec:
                _bus.Write(address, SetNegativeZero(_bus.Read(address) - 1));
                break;
            case Operation.Inx:
                X = SetNegativeZero(X + 1);
                break;
            case Operation.Iny:
                Y = SetNegativeZero(Y + 1);
                break;
            case Operation.Dex:
                X = SetNegativeZero(X - 1);
                break;
            case Operation.Dey:
                Y = SetNegativeZero(Y - 1);
                break;
        }
    }

    private void Control(Operation operation, int address)
    {
        switch (operation)
        {
            case Operation.Jmp:
                PC = address;
                break;
            case Operation.Jsr:
                // JSR pushes the address of its own last byte.
                int last = Wrap(PC - 1);
                Push(last >> 8);
                Push(last & 0xFF);
                PC = address;
                break;
            case Operation.Rts:
                int low = Pull();
                PC = Wrap((low | (Pull() << 8)) + 1);
                break;
            case Operation.Rti:
                Status = (byte)Pull();
                int returnLow = Pull();
                PC = returnLow | (Pull() << 8);
                break;
            case Operation.Brk:
                int next = Wrap(PC + 1);
                Push(next >> 8);
                Push(next & 0xFF);
                Push(Status | BreakFlag);
                InterruptDisable = true;
                PC = ReadWord(IrqVector);
                break;
            case Operation.Pha:
                Push(A);
                break;
            case Operation.Php:
                Push(Status | BreakFlag);
                break;
            case Operation.Pla:
                A = SetNegativeZero(Pull());
                break;
            case Operation.Plp:
                Status = (byte)Pull();
                break;
            case Operation.Clc:
                Carry = false;
                break;
            case Operation.Sec:
                Carry = true;
                break;
            case Operation.Cld:
                DecimalMode = false;
                break;
            case Operation.Sed:
                DecimalMode = true;
                break;
            case Operation.Cli:
                InterruptDisable = false;
                break;
            case Operation.Sei:
                InterruptDisable = true;
                break;
            case Operation.Clv:
                Overflow = false;
                break;
            default:
                throw new SharpKindException($"Operation {operation} is not implemented.");
        }
    }

    private int Branch(Operation operation, int target)
    {
        bool taken = operation switch
        {
            Operation.Bcc => !Carry,
            Operation.Bcs => Carry,
            Operation.Beq => Zero,
            Operation.Bmi => Negative,
            Operation.Bne => !Zero,
            Operation.Bpl => !Negative,
            Operation.Bvc => !Overflow,
            _ => Overflow,
        };

        if (!taken)
        {
            return 0;
        }

        int extra = (PC & 0xFF00) != (target & 0xFF00) ? 2 : 1;
        PC = target;
        return extra;
    }

    private void Shift(Operation operation, Mode mode, int address)
    {
        int value = mode == Mode.Accumulator ? A : _bus.Read(address);
        int carryIn = Carry ? 1 : 0;
        int result;

        switch (operation)
        {
            case Operation.Asl:
                Carry = (value & 0x80) != 0;
                result = value << 1;
                break;
            case Operation.Lsr:
                Carry = (value & 0x01) != 0;
                result = value >> 1;
                break;
            case Operation.Rol:
                Carry = (value & 0x80) != 0;
                result = (value << 1) | carryIn;
                break;
            default:
                Carry = (value & 0x01) != 0;
                result = (value >> 1) | (carryIn << 7);
                break;
        }

        byte shifted = SetNegativeZero(result);

        if (mode == Mode.Accumulator)
        {
            A = shifted;
        }
        else
        {
            _bus.Write(address, shifted);
        }
    }

    private void Compare(int register, int value)
    {
        Carry = register >= value;
        SetNegativeZero(register - value);
    }

    private void Add(int operand)
    {
        int carryIn = Carry ? 1 : 0;
        int binary = A + operand + carryIn;

        if (!DecimalMode)
        {
            Overflow = ((A ^ binary) & (operand ^ binary) & 0x80) != 0;
            Carry = binary > 0xFF;
            A = SetNegativeZero(binary);
            return;
        }

        // NMOS decimal mode: Z comes from the binary sum, N and V from the half-corrected one.
        int low = (A & 0x0F) + (operand & 0x0F) + carryIn;

        if (low >= 0x0A)
        {
            low = ((low + 0x06) & 0x0F) + 0x10;
        }

        int sum = (A & 0xF0) + (operand & 0xF0) + low;

        Zero = (binary & 0xFF) == 0;
        Negative = (sum & 0x80) != 0;
        Overflow = ((A ^ sum) & (operand ^ sum) & 0x80) != 0;

        if (sum >= 0xA0)
        {
            sum += 0x60;
        }

        Carry = sum >= 0x100;
        A = (byte)sum;
    }

    private void Subtract(int operand)
    {
        int borrow = Carry ? 0 : 1;
        int binary = A - operand - borrow;

        Overflow = ((A ^ operand) & (A ^ binary) & 0x80) != 0;
        Carry = binary >= 0;

        if (!DecimalMode)
        {
            A = SetNegativeZero(binary);
            return;
        }

        SetNegativeZero(binary);

        int low = (A & 0x0F) - (operand & 0x0F) - borrow;

        if (low < 0)
        {
            low = ((low - 0x06) & 0x0F) - 0x10;
        }

        int result = (A & 0xF0) - (operand & 0xF0) + low;

        if (result < 0)
        {
            result -= 0x60;
        }

        A = (byte)result;
    }
}
