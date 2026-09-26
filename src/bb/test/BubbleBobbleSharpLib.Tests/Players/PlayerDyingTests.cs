// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Bubbles;
using BubbleBobbleSharpLib.Levels;
using BubbleBobbleSharpLib.Players;
using Xunit;

namespace BubbleBobbleSharpLib.Tests.Players;

// $28A5 and $28E8, and $1D03's second call, worked by hand off the reference.
public sealed class PlayerDyingTests
{
    // $28AD to $28CE. $20 then one step: $1F, whose bits 1 and 2 are 3, so frame $0E. On the ground,
    // so no fall; and the flash is cleared.
    [Fact]
    public void TheFirstCallStartsTheSpin()
    {
        EntityTable entities = Dying(grounded: true);
        entities.FlashTimer[0] = 0x40;

        new PlayerDying(entities).Step(0);

        Assert.Equal(0x12, entities.LeapFlag[0]);
        Assert.Equal(0x1F, entities.AnimationTimer[0]);
        Assert.Equal(0x0E, entities.Frame[0]);
        Assert.Equal(0x80, entities.Y[0]);
        Assert.Equal(0x00, entities.FlashTimer[0]);
    }

    // $28DB. Caught in the air: three pixels a call, and $F3 + 3 wraps to the top.
    [Theory]
    [InlineData(0x80, 0x83)]
    [InlineData(0xF1, 0xF4)]
    [InlineData(0xF3, 0x15)]
    public void APlayerCaughtInTheAirFalls(byte y, byte expected)
    {
        EntityTable entities = Dying(grounded: false);
        entities.Y[0] = y;

        new PlayerDying(entities).Step(0);

        Assert.Equal(expected, entities.Y[0]);
    }

    // $28E9. $20 down to $C8 is 88 calls; the last one moves the state on, in frame $0F.
    [Fact]
    public void ThePlayerIsDeadAfterEightyEightCalls()
    {
        EntityTable entities = Dying(grounded: true);
        PlayerDying dying = new(entities);

        for (int call = 1; call < 88; call++)
        {
            dying.Step(0);
            Assert.Equal(PlayerFrame.DyingState, entities.State[0]);
        }

        dying.Step(0);

        Assert.Equal(PlayerFrame.DeadState, entities.State[0]);
        Assert.Equal(0xC8, entities.AnimationTimer[0]);
        Assert.Equal(0x0F, entities.Frame[0]);
    }

    // $1D03. With the flash set and bit 1 of $08 clear, $1E6C runs twice: $20, $1F, $1E.
    [Theory]
    [InlineData(0x00, 0x1E)]
    [InlineData(0x02, 0x1F)]
    public void TheFlashCanStepTheFirstCallTwice(byte counter, byte expected)
    {
        EntityTable entities = Dying(grounded: true);
        entities.FlashTimer[0] = 0x40;
        Frame(entities).Slot(0, Input.Idle, counter, Open());

        Assert.Equal(expected, entities.AnimationTimer[0]);
    }

    // $28E8. Dead is a bare `rts`.
    [Fact]
    public void ADeadPlayerIsLeftAlone()
    {
        EntityTable entities = Dying(grounded: false);
        entities.State[0] = PlayerFrame.DeadState;

        Frame(entities).Step(0, Input.Idle, Open());

        Assert.Equal(0x80, entities.Y[0]);
        Assert.Equal(0x00, entities.AnimationTimer[0]);
    }

    private static SolidMap Open()
        => SolidMap.Build(new() { Number = 1, Bitmap = [.. Enumerable.Repeat(new string('.', 32), 23)] }, []);

    private static PlayerFrame Frame(EntityTable entities)
    {
        BubbleBlow blow = TestBlow.Of(entities);
        PlayerSteer steer = new(entities, blow);
        PlayerDescent descent = new(entities);

        return new(
            entities,
            new PlayerMovement(entities, blow, TestBlow.NoRings()),
            new PlayerJump(entities, new PlayerDrift(entities, steer, TestBlow.NoRings()), new PlayerLanding(entities, descent)),
            new PlayerFall(entities, steer),
            blow);
    }

    private static EntityTable Dying(bool grounded)
    {
        EntityTable entities = new();
        entities.State[0] = PlayerFrame.DyingState;
        entities.X[0] = 0x40;
        entities.Y[0] = 0x80;
        entities.RiseCounter[0] = 0xFF;
        entities.GroundState[0] = grounded ? (byte)0xFF : (byte)0x00;

        return entities;
    }
}
