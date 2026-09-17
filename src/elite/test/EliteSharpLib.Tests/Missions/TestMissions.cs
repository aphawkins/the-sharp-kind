// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Missions.Classic;
using EliteSharpLib.Missions;
using EliteSharpLib.Ships;
using EliteSharpLib.Trader;
using EliteSharpLib.Types;
using Microsoft.Extensions.Logging.Abstractions;

namespace EliteSharpLib.Tests.Missions;

// Handed straight to a registry rather than found on disk, for tests about what the missions do rather than the loading path.
internal static class TestMissions
{
    internal static MissionRegistry Registry()
        => new([new ConstrictorMission(), new ThargoidMission()], NullLogger<MissionRegistry>.Instance);

    internal static MissionRunner Runner(GameState gameState, PlayerShip ship, Trade trade)
        => new(gameState, ship, trade, Registry(), new PlanetController(gameState));

    /// <summary>
    /// The seed of the numbered galaxy, which is the first one's rotated left
    /// once per galaxy jumped (Space.EnterNextGalaxy). Tests that put the
    /// commander in a later galaxy need this, because a planet number only
    /// means anything against the galaxy it is numbered in.
    /// </summary>
    /// <param name="galaxyNumber">The galaxy, counted from 0.</param>
    /// <returns>Its seed.</returns>
    internal static GalaxySeed GalaxyAt(int galaxyNumber)
    {
        GalaxySeed galaxy = new() { A = 0x4a, B = 0x5a, C = 0x48, D = 0x02, E = 0x53, F = 0xb7 };

        for (int i = 0; i < galaxyNumber; i++)
        {
            galaxy = new()
            {
                A = Rotate(galaxy.A),
                B = Rotate(galaxy.B),
                C = Rotate(galaxy.C),
                D = Rotate(galaxy.D),
                E = Rotate(galaxy.E),
                F = Rotate(galaxy.F),
            };
        }

        return galaxy;
    }

    private static int Rotate(int value) => ((value << 1) | ((value >> 7) & 1)) & 0xFF;
}
