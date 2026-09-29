// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Emulation.Cpu;

public sealed partial class Cpu6502
{
    private static readonly Opcode[] s_table = BuildTable();

    private enum Mode
    {
        Implied = 0,
        Accumulator = 1,
        Immediate = 2,
        ZeroPage = 3,
        ZeroPageX = 4,
        ZeroPageY = 5,
        Absolute = 6,
        AbsoluteX = 7,
        AbsoluteY = 8,
        IndirectX = 9,
        IndirectY = 10,
        Indirect = 11,
        Relative = 12,
    }

    private enum Operation
    {
        Invalid = 0,
        Adc = 1,
        And = 2,
        Asl = 3,
        Bcc = 4,
        Bcs = 5,
        Beq = 6,
        Bit = 7,
        Bmi = 8,
        Bne = 9,
        Bpl = 10,
        Brk = 11,
        Bvc = 12,
        Bvs = 13,
        Clc = 14,
        Cld = 15,
        Cli = 16,
        Clv = 17,
        Cmp = 18,
        Cpx = 19,
        Cpy = 20,
        Dec = 21,
        Dex = 22,
        Dey = 23,
        Eor = 24,
        Inc = 25,
        Inx = 26,
        Iny = 27,
        Jmp = 28,
        Jsr = 29,
        Lda = 30,
        Ldx = 31,
        Ldy = 32,
        Lsr = 33,
        Nop = 34,
        Ora = 35,
        Pha = 36,
        Php = 37,
        Pla = 38,
        Plp = 39,
        Rol = 40,
        Ror = 41,
        Rti = 42,
        Rts = 43,
        Sbc = 44,
        Sec = 45,
        Sed = 46,
        Sei = 47,
        Sta = 48,
        Stx = 49,
        Sty = 50,
        Tax = 51,
        Tay = 52,
        Tsx = 53,
        Txa = 54,
        Txs = 55,
        Tya = 56,
    }

    private static Opcode[] BuildTable()
    {
        Opcode[] table = new Opcode[256];

        void Add(int code, Operation operation, Mode mode, int cycles, bool pagePenalty = false)
            => table[code] = new Opcode(operation, mode, cycles, pagePenalty);

        // The eight-mode groups: immediate, zero page, zero page X, absolute, absolute X, absolute Y, (indirect,X),
        // (indirect),Y. Reads pay a cycle for a page cross; stores never do and always take the long time.
        void Group(
            Operation operation,
            int immediate,
            int zeroPage,
            int zeroPageX,
            int absolute,
            int absoluteX,
            int absoluteY,
            int indirectX,
            int indirectY)
        {
            Add(immediate, operation, Mode.Immediate, 2);
            Add(zeroPage, operation, Mode.ZeroPage, 3);
            Add(zeroPageX, operation, Mode.ZeroPageX, 4);
            Add(absolute, operation, Mode.Absolute, 4);
            Add(absoluteX, operation, Mode.AbsoluteX, 4, true);
            Add(absoluteY, operation, Mode.AbsoluteY, 4, true);
            Add(indirectX, operation, Mode.IndirectX, 6);
            Add(indirectY, operation, Mode.IndirectY, 5, true);
        }

        void Shift(Operation operation, int accumulator, int zeroPage, int zeroPageX, int absolute, int absoluteX)
        {
            Add(accumulator, operation, Mode.Accumulator, 2);
            Add(zeroPage, operation, Mode.ZeroPage, 5);
            Add(zeroPageX, operation, Mode.ZeroPageX, 6);
            Add(absolute, operation, Mode.Absolute, 6);
            Add(absoluteX, operation, Mode.AbsoluteX, 7);
        }

        void Implied(Operation operation, int code, int cycles = 2) => Add(code, operation, Mode.Implied, cycles);

        Group(Operation.Adc, 0x69, 0x65, 0x75, 0x6D, 0x7D, 0x79, 0x61, 0x71);
        Group(Operation.And, 0x29, 0x25, 0x35, 0x2D, 0x3D, 0x39, 0x21, 0x31);
        Group(Operation.Cmp, 0xC9, 0xC5, 0xD5, 0xCD, 0xDD, 0xD9, 0xC1, 0xD1);
        Group(Operation.Eor, 0x49, 0x45, 0x55, 0x4D, 0x5D, 0x59, 0x41, 0x51);
        Group(Operation.Lda, 0xA9, 0xA5, 0xB5, 0xAD, 0xBD, 0xB9, 0xA1, 0xB1);
        Group(Operation.Ora, 0x09, 0x05, 0x15, 0x0D, 0x1D, 0x19, 0x01, 0x11);
        Group(Operation.Sbc, 0xE9, 0xE5, 0xF5, 0xED, 0xFD, 0xF9, 0xE1, 0xF1);

        Add(0x85, Operation.Sta, Mode.ZeroPage, 3);
        Add(0x95, Operation.Sta, Mode.ZeroPageX, 4);
        Add(0x8D, Operation.Sta, Mode.Absolute, 4);
        Add(0x9D, Operation.Sta, Mode.AbsoluteX, 5);
        Add(0x99, Operation.Sta, Mode.AbsoluteY, 5);
        Add(0x81, Operation.Sta, Mode.IndirectX, 6);
        Add(0x91, Operation.Sta, Mode.IndirectY, 6);

        Shift(Operation.Asl, 0x0A, 0x06, 0x16, 0x0E, 0x1E);
        Shift(Operation.Lsr, 0x4A, 0x46, 0x56, 0x4E, 0x5E);
        Shift(Operation.Rol, 0x2A, 0x26, 0x36, 0x2E, 0x3E);
        Shift(Operation.Ror, 0x6A, 0x66, 0x76, 0x6E, 0x7E);

        Add(0xC6, Operation.Dec, Mode.ZeroPage, 5);
        Add(0xD6, Operation.Dec, Mode.ZeroPageX, 6);
        Add(0xCE, Operation.Dec, Mode.Absolute, 6);
        Add(0xDE, Operation.Dec, Mode.AbsoluteX, 7);
        Add(0xE6, Operation.Inc, Mode.ZeroPage, 5);
        Add(0xF6, Operation.Inc, Mode.ZeroPageX, 6);
        Add(0xEE, Operation.Inc, Mode.Absolute, 6);
        Add(0xFE, Operation.Inc, Mode.AbsoluteX, 7);

        Add(0xA2, Operation.Ldx, Mode.Immediate, 2);
        Add(0xA6, Operation.Ldx, Mode.ZeroPage, 3);
        Add(0xB6, Operation.Ldx, Mode.ZeroPageY, 4);
        Add(0xAE, Operation.Ldx, Mode.Absolute, 4);
        Add(0xBE, Operation.Ldx, Mode.AbsoluteY, 4, true);
        Add(0xA0, Operation.Ldy, Mode.Immediate, 2);
        Add(0xA4, Operation.Ldy, Mode.ZeroPage, 3);
        Add(0xB4, Operation.Ldy, Mode.ZeroPageX, 4);
        Add(0xAC, Operation.Ldy, Mode.Absolute, 4);
        Add(0xBC, Operation.Ldy, Mode.AbsoluteX, 4, true);
        Add(0x86, Operation.Stx, Mode.ZeroPage, 3);
        Add(0x96, Operation.Stx, Mode.ZeroPageY, 4);
        Add(0x8E, Operation.Stx, Mode.Absolute, 4);
        Add(0x84, Operation.Sty, Mode.ZeroPage, 3);
        Add(0x94, Operation.Sty, Mode.ZeroPageX, 4);
        Add(0x8C, Operation.Sty, Mode.Absolute, 4);

        Add(0xE0, Operation.Cpx, Mode.Immediate, 2);
        Add(0xE4, Operation.Cpx, Mode.ZeroPage, 3);
        Add(0xEC, Operation.Cpx, Mode.Absolute, 4);
        Add(0xC0, Operation.Cpy, Mode.Immediate, 2);
        Add(0xC4, Operation.Cpy, Mode.ZeroPage, 3);
        Add(0xCC, Operation.Cpy, Mode.Absolute, 4);
        Add(0x24, Operation.Bit, Mode.ZeroPage, 3);
        Add(0x2C, Operation.Bit, Mode.Absolute, 4);

        Add(0x10, Operation.Bpl, Mode.Relative, 2);
        Add(0x30, Operation.Bmi, Mode.Relative, 2);
        Add(0x50, Operation.Bvc, Mode.Relative, 2);
        Add(0x70, Operation.Bvs, Mode.Relative, 2);
        Add(0x90, Operation.Bcc, Mode.Relative, 2);
        Add(0xB0, Operation.Bcs, Mode.Relative, 2);
        Add(0xD0, Operation.Bne, Mode.Relative, 2);
        Add(0xF0, Operation.Beq, Mode.Relative, 2);

        Add(0x4C, Operation.Jmp, Mode.Absolute, 3);
        Add(0x6C, Operation.Jmp, Mode.Indirect, 5);
        Add(0x20, Operation.Jsr, Mode.Absolute, 6);
        Implied(Operation.Rts, 0x60, 6);
        Implied(Operation.Rti, 0x40, 6);
        Implied(Operation.Brk, 0x00, 7);

        Implied(Operation.Pha, 0x48, 3);
        Implied(Operation.Php, 0x08, 3);
        Implied(Operation.Pla, 0x68, 4);
        Implied(Operation.Plp, 0x28, 4);

        Implied(Operation.Clc, 0x18);
        Implied(Operation.Cld, 0xD8);
        Implied(Operation.Cli, 0x58);
        Implied(Operation.Clv, 0xB8);
        Implied(Operation.Sec, 0x38);
        Implied(Operation.Sed, 0xF8);
        Implied(Operation.Sei, 0x78);
        Implied(Operation.Dex, 0xCA);
        Implied(Operation.Dey, 0x88);
        Implied(Operation.Inx, 0xE8);
        Implied(Operation.Iny, 0xC8);
        Implied(Operation.Nop, 0xEA);
        Implied(Operation.Tax, 0xAA);
        Implied(Operation.Tay, 0xA8);
        Implied(Operation.Tsx, 0xBA);
        Implied(Operation.Txa, 0x8A);
        Implied(Operation.Txs, 0x9A);
        Implied(Operation.Tya, 0x98);

        return table;
    }

    private readonly record struct Opcode(Operation Operation, Mode Mode, int Cycles, bool PagePenalty);
}
