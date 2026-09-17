// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharp.Abstractions.Missions;
using Microsoft.Extensions.Logging;

namespace EliteSharpLib.Missions;

/// <summary>
/// Every mission the game has, whether it was built in or found in a plugin,
/// looked up by the name the save file records. Built once at startup and read
/// from everywhere after, which is what lets the rest of the game take a
/// mission name from a save file and get either the mission or a straight
/// answer that there is none.
/// </summary>
internal sealed class MissionRegistry
{
    private readonly Dictionary<string, IMission> _missions;

    /// <summary>
    /// Initializes a new instance of the <see cref="MissionRegistry"/> class.
    /// </summary>
    /// <param name="missions">The missions found, in any order.</param>
    /// <param name="logger">Where a clash is reported before it is thrown.</param>
    /// <exception cref="InvalidOperationException">
    /// Two missions answer to one name, so a save file naming it could mean
    /// either. There is no safe way to carry on, and which plugin to remove is
    /// the only useful thing to say.
    /// </exception>
    public MissionRegistry(IEnumerable<IMission> missions, ILogger<MissionRegistry> logger)
    {
        // Ordinal: a mission name is a save-file key, compared that way throughout the contract.
        _missions = new(StringComparer.Ordinal);

        foreach (IMission mission in missions)
        {
            if (!_missions.TryAdd(mission.Name, mission))
            {
                string first = _missions[mission.Name].GetType().Assembly.GetName().Name ?? "?";
                string second = mission.GetType().Assembly.GetName().Name ?? "?";
                LogMessages.DuplicateMissionName(logger, mission.Name, first, second);

                throw new InvalidOperationException(
                    $"'{first}' and '{second}' both provide a mission called '{mission.Name}'. Remove one of them.");
            }
        }
    }

    /// <summary>
    /// Gets every mission, for the parts of the game that must ask all of them
    /// - what a system's rumours are, what is waiting in open space.
    /// </summary>
    public IReadOnlyCollection<IMission> All => _missions.Values;

    /// <summary>
    /// The mission of that name, or null when none is installed - which is what
    /// a save file naming a mission whose plugin has been removed comes back
    /// as.
    /// </summary>
    /// <param name="name">The <see cref="IMission.Name"/> to look up.</param>
    /// <returns>The mission, or null.</returns>
    public IMission? Find(string name) => _missions.GetValueOrDefault(name);
}
