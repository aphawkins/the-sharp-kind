// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

namespace EliteSharpLib.Conflict;

/// <summary>
/// Decides which ships think on which count, so that one universe slot in
/// eight runs its tactics and no two slots in the same eight think together.
/// </summary>
/// <remarks>
/// <para>
/// The original spread the thinking by phase: a ship thinks on the counts
/// whose low three bits match its own slot, which costs a constant amount of
/// work per count and keeps the cost even. That is
/// <c>((slot ^ count) &amp; 7) == 0</c>, and it is kept exactly.
/// </para>
/// <para>
/// What it cannot do on its own any more is stay once. The count used to be
/// an update; it now moves 13.5 times a second whatever the game is updated
/// at, so at a faster rate the phase test stays true for several updates
/// together and a ship would think four or five times where it thought once
/// - free reactions, bought with frame rate. So the count each slot last
/// thought on is remembered, and a repeat within the same count is refused.
/// </para>
/// <para>
/// The memory cannot hold a ship back. A slot's next matching count is eight
/// on, and the same value only returns after a full turn of the counter, by
/// which time that slot has thought on thirty-one other counts.
/// </para>
/// </remarks>
internal sealed class TacticsSchedule
{
    /// <summary>
    /// One slot in this many thinks on any given count.
    /// </summary>
    private const int SlotsPerCount = 8;

    /// <summary>
    /// The counter runs 0..255, so this marks a slot that has yet to think
    /// and cannot be mistaken for one that already has.
    /// </summary>
    private const int NoThoughtYet = -1;

    // Indexed the way Space walks the universe: planet, then station or sun, then ships.
    private readonly int[] _lastThoughtOn = new int[Universe.MaxUniverseObjects + 2];

    internal TacticsSchedule() => Reset();

    /// <summary>
    /// Whether the ship in <paramref name="slot"/> should run its tactics on
    /// <paramref name="count"/>, recording it if so.
    /// </summary>
    internal bool ShouldThink(int slot, int count)
    {
        if (((slot ^ count) & (SlotsPerCount - 1)) != 0 || _lastThoughtOn[slot] == count)
        {
            return false;
        }

        _lastThoughtOn[slot] = count;
        return true;
    }

    /// <summary>
    /// Forgets every slot, for when the universe is emptied.
    /// </summary>
    /// <remarks>
    /// A slot outlives the ship in it, so without this a newcomer would
    /// inherit whenever the last occupant of its slot happened to think.
    /// </remarks>
    internal void Reset() => Array.Fill(_lastThoughtOn, NoThoughtYet);
}
