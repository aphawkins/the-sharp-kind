// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

internal sealed class ItemEffects
{
    private const byte FastReload = 0x03;
    private const byte FarState = 0x8C;
    private const byte FastVariant = 0xFF;
    private const byte CandyType = 0xFF;

    private const byte FinalFlash = 0xFE;

    private static readonly ushort[] s_handlers =
    [
        0x7FF3, 0x348A, 0x3495, 0x2DAB, 0x2DB2, 0x2DCD, 0xA64D, 0xA653, 0x2DD7, 0x2DBE, 0x2DC7, 0x2ECC,
        0x2F5F, 0x2F62, 0x2F65, 0x2F68, 0x2F7D, 0x2F83, 0x2F89, 0x3620, 0x7FA4, 0x348D, 0x7FA7, 0x3498,
        0x34A0, 0x34FA, 0x3502, 0x350A, 0x35A7, 0x35B0, 0x3620, 0x3620, 0x3621, 0x7C37, 0x7C3C,
    ];

    private readonly PlayerTable _players;
    private readonly EntityTable _entities;

    internal ItemEffects(PlayerTable players, EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(players);
        ArgumentNullException.ThrowIfNull(entities);

        _players = players;
        _entities = entities;
    }

    internal void Apply(int type, int player)
    {
        switch (type)
        {
            case 0:
                _entities.FlashTimer[player] = FinalFlash;
                break;
            case 3:
                Reload(player);
                break;
            case 4:
                Travel(player);
                break;
            case 5:
                Kind(player);
                break;
            case 9:
                _players.DriftRing[player]--;
                _players.WalkRing[player]--;
                _players.BlowRing[player]--;
                Candies(player);
                break;
            case 10:
                Candies(player);
                break;
            case 12:
                _players.DriftRing[player]--;
                break;
            case 13:
                _players.WalkRing[player]--;
                break;
            case 14:
                _players.BlowRing[player]--;
                break;
            default:
                throw new NotSupportedException($"special item {type}'s effect at ${s_handlers[type]:X4} is not translated");
        }
    }

    private void Candies(int player)
    {
        Reload(player);
        Travel(player);
        Kind(player);
    }

    private void Reload(int player) => _players.BlowReload[player] = FastReload;

    private void Travel(int player)
    {
        _players.BlowState[player] = FarState;
        _players.BlowVariant[player] = FastVariant;
    }

    private void Kind(int player) => _players.BlowType[player] = CandyType;
}
