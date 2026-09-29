// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using SharpKind.Emulation.Cpu;

namespace SharpKind.Emulation.Tests.Cpu;

public class Cpu6502Tests
{
    private const int Origin = 0x0200;

    [Fact]
    public void LoadsSetNegativeAndZero()
    {
        // Arrange
        (Cpu6502 cpu, Ram ram) = Load(0xA9, 0x00, 0xA2, 0x80);

        // Act
        cpu.Step();
        bool zero = cpu.Zero;
        cpu.Step();

        // Assert
        Assert.True(zero);
        Assert.Equal(0x80, cpu.X);
        Assert.True(cpu.Negative);
        Assert.False(cpu.Zero);
        Assert.Equal(Origin + 4, cpu.PC);
        Assert.NotNull(ram);
    }

    [Fact]
    public void ZeroPageIndexingWrapsWithinThePage()
    {
        // Arrange: LDX #$10, LDA $F5,X reads $05, not $105.
        (Cpu6502 cpu, Ram ram) = Load(0xA2, 0x10, 0xB5, 0xF5);
        ram.Poke(0x0005, 0x42);
        ram.Poke(0x0105, 0x99);

        // Act
        cpu.Step();
        int cycles = cpu.Step();

        // Assert
        Assert.Equal(0x42, cpu.A);
        Assert.Equal(4, cycles);
    }

    [Fact]
    public void AbsoluteIndexingPaysACycleForACrossedPage()
    {
        // Arrange: LDX #$01, LDA $02FF,X
        (Cpu6502 cpu, Ram ram) = Load(0xA2, 0x01, 0xBD, 0xFF, 0x02);
        ram.Poke(0x0300, 0x77);

        // Act
        cpu.Step();
        int cycles = cpu.Step();

        // Assert
        Assert.Equal(0x77, cpu.A);
        Assert.Equal(5, cycles);
    }

    [Fact]
    public void StoresNeverPayThePageCycleButTakeTheLongTime()
    {
        // Arrange: LDX #$01, LDA #$55, STA $02FF,X
        (Cpu6502 cpu, Ram ram) = Load(0xA2, 0x01, 0xA9, 0x55, 0x9D, 0xFF, 0x02);

        // Act
        cpu.Step();
        cpu.Step();
        int cycles = cpu.Step();

        // Assert
        Assert.Equal(5, cycles);
        Assert.Equal(0x55, ram.Read(0x0300));
    }

    [Fact]
    public void IndirectIndexedReadsThroughTheZeroPagePointer()
    {
        // Arrange: LDY #$03, LDA ($20),Y
        (Cpu6502 cpu, Ram ram) = Load(0xA0, 0x03, 0xB1, 0x20);
        ram.Poke(0x20, 0x00);
        ram.Poke(0x21, 0x03);
        ram.Poke(0x0303, 0x5A);

        // Act
        cpu.Step();
        cpu.Step();

        // Assert
        Assert.Equal(0x5A, cpu.A);
    }

    [Fact]
    public void IndexedIndirectWrapsThePointerInTheZeroPage()
    {
        // Arrange: LDX #$FF, LDA ($00,X) uses the pointer at $FF/$00.
        (Cpu6502 cpu, Ram ram) = Load(0xA2, 0xFF, 0xA1, 0x00);
        ram.Poke(0x00FF, 0x00);
        ram.Poke(0x0000, 0x03);
        ram.Poke(0x0300, 0x31);

        // Act
        cpu.Step();
        cpu.Step();

        // Assert
        Assert.Equal(0x31, cpu.A);
    }

    [Fact]
    public void IndirectJumpDoesNotCarryIntoTheNextPage()
    {
        // Arrange: JMP ($02FF) takes its high byte from $0200.
        (Cpu6502 cpu, Ram ram) = Load(0x6C, 0xFF, 0x02);
        ram.Poke(0x02FF, 0x34);
        ram.Poke(0x0300, 0x99);

        // Act
        cpu.Step();

        // Assert: $0200 holds the opcode 0x6C.
        Assert.Equal(0x6C34, cpu.PC);
    }

    [Theory]
    [InlineData(0x50, 0x50, false, 0xA0, false, true, true)]
    [InlineData(0xD0, 0x90, false, 0x60, true, true, false)]
    [InlineData(0xFF, 0x01, false, 0x00, true, false, false)]
    [InlineData(0x01, 0x01, true, 0x03, false, false, false)]
    public void AddWithCarryBinary(byte a, byte operand, bool carryIn, byte expected, bool carry, bool overflow, bool negative)
    {
        // Arrange
        (Cpu6502 cpu, _) = Load(0x69, operand);
        cpu.A = a;
        cpu.Carry = carryIn;

        // Act
        cpu.Step();

        // Assert
        Assert.Equal(expected, cpu.A);
        Assert.Equal(carry, cpu.Carry);
        Assert.Equal(overflow, cpu.Overflow);
        Assert.Equal(negative, cpu.Negative);
    }

    [Theory]
    [InlineData(0x09, 0x01, false, 0x10, false)]
    [InlineData(0x99, 0x01, false, 0x00, true)]
    [InlineData(0x58, 0x46, true, 0x05, true)]
    public void AddWithCarryDecimal(byte a, byte operand, bool carryIn, byte expected, bool carry)
    {
        // Arrange
        (Cpu6502 cpu, _) = Load(0x69, operand);
        cpu.A = a;
        cpu.Carry = carryIn;
        cpu.DecimalMode = true;

        // Act
        cpu.Step();

        // Assert
        Assert.Equal(expected, cpu.A);
        Assert.Equal(carry, cpu.Carry);
    }

    [Theory]
    [InlineData(0x50, 0x10, true, 0x40, true, false)]
    [InlineData(0x50, 0x60, true, 0xF0, false, true)]
    [InlineData(0x50, 0xB0, true, 0xA0, false, true)]
    [InlineData(0x00, 0x00, false, 0xFF, false, true)]
    public void SubtractWithCarryBinary(byte a, byte operand, bool carryIn, byte expected, bool carry, bool overflowOrNegative)
    {
        // Arrange
        (Cpu6502 cpu, _) = Load(0xE9, operand);
        cpu.A = a;
        cpu.Carry = carryIn;

        // Act
        cpu.Step();

        // Assert
        Assert.Equal(expected, cpu.A);
        Assert.Equal(carry, cpu.Carry);
        Assert.Equal(overflowOrNegative, cpu.Overflow || cpu.Negative);
    }

    [Theory]
    [InlineData(0x10, 0x01, 0x09, true)]
    [InlineData(0x00, 0x01, 0x99, false)]
    [InlineData(0x46, 0x12, 0x34, true)]
    public void SubtractWithCarryDecimal(byte a, byte operand, byte expected, bool carry)
    {
        // Arrange
        (Cpu6502 cpu, _) = Load(0xE9, operand);
        cpu.A = a;
        cpu.Carry = true;
        cpu.DecimalMode = true;

        // Act
        cpu.Step();

        // Assert
        Assert.Equal(expected, cpu.A);
        Assert.Equal(carry, cpu.Carry);
    }

    [Fact]
    public void CompareSetsCarryWhenTheRegisterIsNotBelow()
    {
        // Arrange: CMP #$40, CMP #$41, CMP #$40 with A = $40 throughout.
        (Cpu6502 cpu, _) = Load(0xC9, 0x40, 0xC9, 0x41, 0xC9, 0x3F);
        cpu.A = 0x40;

        // Act
        cpu.Step();
        (bool carryEqual, bool zeroEqual) = (cpu.Carry, cpu.Zero);
        cpu.Step();
        (bool carryBelow, bool negativeBelow) = (cpu.Carry, cpu.Negative);
        cpu.Step();

        // Assert
        Assert.True(carryEqual);
        Assert.True(zeroEqual);
        Assert.False(carryBelow);
        Assert.True(negativeBelow);
        Assert.True(cpu.Carry);
        Assert.False(cpu.Zero);
    }

    [Fact]
    public void BitCopiesTopBitsAndTestsAgainstTheAccumulator()
    {
        // Arrange
        (Cpu6502 cpu, Ram ram) = Load(0x24, 0x10);
        ram.Poke(0x10, 0xC0);
        cpu.A = 0x01;

        // Act
        cpu.Step();

        // Assert
        Assert.True(cpu.Negative);
        Assert.True(cpu.Overflow);
        Assert.True(cpu.Zero);
    }

    [Fact]
    public void ShiftsAndRotatesMoveTheCarry()
    {
        // Arrange: ASL A, ROL A, LSR A, ROR A from A = $81
        (Cpu6502 cpu, _) = Load(0x0A, 0x2A, 0x4A, 0x6A);
        cpu.A = 0x81;

        // Act
        cpu.Step();
        (byte afterAsl, bool carryAsl) = (cpu.A, cpu.Carry);
        cpu.Step();
        (byte afterRol, bool carryRol) = (cpu.A, cpu.Carry);
        cpu.Step();
        (byte afterLsr, bool carryLsr) = (cpu.A, cpu.Carry);
        cpu.Step();

        // Assert
        Assert.Equal(0x02, afterAsl);
        Assert.True(carryAsl);
        Assert.Equal(0x05, afterRol);
        Assert.False(carryRol);
        Assert.Equal(0x02, afterLsr);
        Assert.True(carryLsr);
        Assert.Equal(0x81, cpu.A);
        Assert.False(cpu.Carry);
    }

    [Fact]
    public void ReadModifyWriteWritesBackToMemory()
    {
        // Arrange: INC $10, DEC $11, ASL $12
        (Cpu6502 cpu, Ram ram) = Load(0xE6, 0x10, 0xC6, 0x11, 0x06, 0x12);
        ram.Poke(0x10, 0xFF);
        ram.Poke(0x11, 0x00);
        ram.Poke(0x12, 0x41);

        // Act
        cpu.Step();
        cpu.Step();
        cpu.Step();

        // Assert
        Assert.Equal(0x00, ram.Read(0x10));
        Assert.Equal(0xFF, ram.Read(0x11));
        Assert.Equal(0x82, ram.Read(0x12));
    }

    [Fact]
    public void JsrPushesTheAddressOfItsLastByteAndRtsReturnsPastIt()
    {
        // Arrange: JSR $0210 at $0200; RTS at $0210.
        (Cpu6502 cpu, Ram ram) = Load(0x20, 0x10, 0x02);
        ram.Poke(0x0210, 0x60);
        cpu.S = 0xFF;

        // Act
        int jsr = cpu.Step();
        (int target, byte stack) = (cpu.PC, cpu.S);
        int rts = cpu.Step();

        // Assert
        Assert.Equal(6, jsr);
        Assert.Equal(0x0210, target);
        Assert.Equal(0xFD, stack);
        Assert.Equal(0x02, ram.Read(0x01FF));
        Assert.Equal(0x02, ram.Read(0x01FE));
        Assert.Equal(6, rts);
        Assert.Equal(0x0203, cpu.PC);
        Assert.Equal(0xFF, cpu.S);
    }

    [Fact]
    public void PhpPushesTheBreakAndUnusedBits()
    {
        // Arrange
        (Cpu6502 cpu, Ram ram) = Load(0x08, 0x28);
        cpu.S = 0xFF;
        cpu.Carry = true;

        // Act
        cpu.Step();
        cpu.Carry = false;
        cpu.Step();

        // Assert
        Assert.Equal(0x31, ram.Read(0x01FF));
        Assert.True(cpu.Carry);
    }

    [Fact]
    public void BranchesCostOneCycleTakenAndTwoAcrossAPage()
    {
        // Arrange: at $02FD, BNE +$10 falls at $02FF and lands on $030F.
        (Cpu6502 cpu, Ram ram) = Load();
        ram.Poke(0x02FD, 0xD0);
        ram.Poke(0x02FE, 0x10);
        ram.Poke(0x0200, 0xD0);
        ram.Poke(0x0201, 0x02);
        cpu.Zero = false;

        // Act
        int near = cpu.Step();
        cpu.PC = 0x02FD;
        int far = cpu.Step();
        cpu.Zero = true;
        cpu.PC = 0x0200;
        int notTaken = cpu.Step();

        // Assert
        Assert.Equal(3, near);
        Assert.Equal(4, far);
        Assert.Equal(2, notTaken);
    }

    [Fact]
    public void BackwardBranchLandsBehindTheInstruction()
    {
        // Arrange: LDX #$03, DEX, BNE -3 (back to DEX)
        (Cpu6502 cpu, _) = Load(0xA2, 0x03, 0xCA, 0xD0, 0xFD);

        // Act
        cpu.Step();
        int steps = 0;

        while (cpu.PC != Origin + 5)
        {
            cpu.Step();
            steps++;
        }

        // Assert
        Assert.Equal(0, cpu.X);
        Assert.Equal(6, steps);
    }

    [Fact]
    public void BrkPushesTheStatusWithTheBreakFlagAndJumpsThroughTheVector()
    {
        // Arrange
        (Cpu6502 cpu, Ram ram) = Load(0x00);
        ram.Poke(0xFFFE, 0x00);
        ram.Poke(0xFFFF, 0x04);
        cpu.S = 0xFF;

        // Act
        cpu.Step();

        // Assert
        Assert.Equal(0x0400, cpu.PC);
        Assert.Equal(0x02, ram.Read(0x01FF));
        Assert.Equal(0x02, ram.Read(0x01FE));
        Assert.Equal(0x30, ram.Read(0x01FD));
        Assert.True(cpu.InterruptDisable);
    }

    [Fact]
    public void IrqPushesThePcAndAStatusWithoutTheBreakFlagAndRtiReturns()
    {
        // Arrange
        (Cpu6502 cpu, Ram ram) = Load(0xEA);
        ram.Poke(0xFFFE, 0x00);
        ram.Poke(0xFFFF, 0x04);
        ram.Poke(0x0400, 0x40);
        cpu.S = 0xFF;

        // Act
        cpu.Irq();
        byte pushed = ram.Read(0x01FD);
        cpu.Step();

        // Assert
        Assert.Equal(0x20, pushed);
        Assert.Equal(Origin, cpu.PC);
        Assert.Equal(0xFF, cpu.S);
    }

    [Fact]
    public void CallRunsARoutineToItsRts()
    {
        // Arrange: LDA #$05, STA $D418, RTS
        (Cpu6502 cpu, Ram ram) = Load(0xA9, 0x05, 0x8D, 0x18, 0xD4, 0x60);

        // Act
        int cycles = cpu.Call(Origin, 0, 100);

        // Assert
        Assert.Equal(2 + 4 + 6, cycles);
        Assert.Equal(0x05, ram.Read(0xD418));
    }

    [Fact]
    public void CallPassesTheRegisters()
    {
        // Arrange: STA $10, STX $11, STY $12, RTS
        (Cpu6502 cpu, Ram ram) = Load(0x85, 0x10, 0x86, 0x11, 0x84, 0x12, 0x60);

        // Act
        cpu.Call(Origin, 1, 100, 2, 3);

        // Assert
        Assert.Equal(1, ram.Read(0x10));
        Assert.Equal(2, ram.Read(0x11));
        Assert.Equal(3, ram.Read(0x12));
    }

    [Fact]
    public void CallThrowsForARoutineThatNeverReturns()
    {
        // Arrange: JMP $0200
        (Cpu6502 cpu, _) = Load(0x4C, 0x00, 0x02);

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(() => cpu.Call(Origin, 0, 100));

        // Assert
        Assert.Contains("ran over", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUndocumentedOpcodeThrowsWithItsAddress()
    {
        // Arrange
        (Cpu6502 cpu, _) = Load(0x02);

        // Act
        SharpKindException exception = Assert.Throws<SharpKindException>(() => cpu.Step());

        // Assert
        Assert.Contains("$02", exception.Message, StringComparison.Ordinal);
        Assert.Contains("$0200", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StackPointerWrapsWithinPageOne()
    {
        // Arrange: PHA at S = 0 leaves S = $FF.
        (Cpu6502 cpu, Ram ram) = Load(0x48);
        cpu.S = 0x00;
        cpu.A = 0x66;

        // Act
        cpu.Step();

        // Assert
        Assert.Equal(0xFF, cpu.S);
        Assert.Equal(0x66, ram.Read(0x0100));
    }

    [Fact]
    public void TransfersMoveRegistersAndTxsLeavesTheFlagsAlone()
    {
        // Arrange: TAX, TXS, TSX, TYA
        (Cpu6502 cpu, _) = Load(0xAA, 0x9A, 0xBA, 0x98);
        cpu.A = 0x80;
        cpu.Y = 0x01;

        // Act
        cpu.Step();
        cpu.Zero = true;
        cpu.Step();
        bool zero = cpu.Zero;
        cpu.Step();
        cpu.Step();

        // Assert
        Assert.True(zero);
        Assert.Equal(0x80, cpu.S);
        Assert.Equal(0x80, cpu.X);
        Assert.Equal(0x01, cpu.A);
    }

    private static (Cpu6502 Cpu, Ram Ram) Load(params int[] code)
    {
        Ram ram = new();
        Cpu6502 cpu = new(ram) { PC = Origin };

        for (int i = 0; i < code.Length; i++)
        {
            ram.Poke(Origin + i, (byte)code[i]);
        }

        return (cpu, ram);
    }

    private sealed class Ram : IBus
    {
        private readonly byte[] _memory = new byte[0x10000];

        public byte Read(int address) => _memory[address & 0xFFFF];

        public void Write(int address, byte value) => _memory[address & 0xFFFF] = value;

        public void Poke(int address, byte value) => _memory[address] = value;
    }
}
