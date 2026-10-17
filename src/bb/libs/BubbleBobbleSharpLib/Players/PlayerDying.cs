// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerDying
{
    private const byte Started = 0x12;

    private const byte SpinCount = 0x20;

    private const byte SpinFrame = 0x0B;
    private const byte StillFrame = 0x0F;

    private const byte DeadCount = 0xC8;

    private const byte FallStep = 0x03;
    private const byte Bottom = 0xF5;
    private const byte Top = 0x15;

    private readonly EntityTable _entities;

    internal PlayerDying(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        _entities = entities;
    }

    internal void Step(int player)
    {
        _entities.Slot(player).FlashTimer = 0;

        if (_entities.Slot(player).LeapFlag != Started)
        {
            _entities.Slot(player).LeapFlag = Started;
            _entities.Slot(player).AnimationTimer = SpinCount;
        }

        byte count = --_entities.Slot(player).AnimationTimer;

        if ((count & 0x80) != 0)
        {
            _entities.Slot(player).Frame = (byte)(((count >> 1) & 0x03) + StillFrame);

            if (count == DeadCount)
            {
                _entities.Slot(player).State++;
            }

            return;
        }

        int frame = (count >> 1) & 0x03;
        _entities.Slot(player).Frame = (byte)(frame == 0 ? 0 : frame + SpinFrame);

        if ((_entities.Slot(player).RiseCounter & 0x80) != 0 && (_entities.Slot(player).GroundState & 0x80) != 0)
        {
            return;
        }

        byte y = (byte)(_entities.Slot(player).Y + FallStep);
        _entities.Slot(player).Y = y >= Bottom ? Top : y;
    }
}
