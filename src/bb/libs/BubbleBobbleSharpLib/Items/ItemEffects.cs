// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Items;

// $2D65 and $2D88 in special-item-effects.s: what the level's special item does to the player who
// takes it. $2D40 looks the handler up by the item's type, 0 to 34, and calls it with the player in X.
//
// The ones translated change what a blow makes, or turn a ring on:
//
//   * 3, $2DAB, a reload of 3 rather than 8 - bubbles as fast as the button.
//   * 4, $2DB2, a bubble that travels further ($8C in $A9B2) and faster ($FF in $AA30).
//   * 5, $2DCD, $FF as the bubble's $AA42.
//   * 10, $2DC7, all three of those; 9, $2DBE, all three and every ring.
//   * 12, 13 and 14, $2F5F, $2F62 and $2F65, one ring each: drift, walk and blow.
//   * 0, final_routine at $7FF3, $FE in the player's $8728.
//
// **Not here:** the rest reach code that is not translated - the level skips, the freeze at $2F74,
// the screen effects, the bonus level's own handlers. They throw with the address, rather than
// pretend. $2DD2's store of 5 in $B0 is not here either: nothing translated reads $B0.
internal sealed class ItemEffects
{
    // $2DAB, $2DB2 and $2DCD.
    private const byte FastReload = 0x03;
    private const byte FarState = 0x8C;
    private const byte FastVariant = 0xFF;
    private const byte CandyType = 0xFF;

    // final_routine.
    private const byte FinalFlash = 0xFE;

    // $2D65's handlers, for the message a throw carries.
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

    // $2D43. type is $53 with its top bit clear.
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

    // $2DC7, falling into $2DCD.
    private void Candies(int player)
    {
        Reload(player);
        Travel(player);
        Kind(player);
    }

    // $2DAB.
    private void Reload(int player) => _players.BlowReload[player] = FastReload;

    // $2DB2.
    private void Travel(int player)
    {
        _players.BlowState[player] = FarState;
        _players.BlowVariant[player] = FastVariant;
    }

    // $2DCD.
    private void Kind(int player) => _players.BlowType[player] = CandyType;
}
