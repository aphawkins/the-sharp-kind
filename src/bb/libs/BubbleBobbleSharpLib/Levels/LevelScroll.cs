// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The scroll to the next level ($37C9-$385E, from $09A5 with the carry clear): the screen moves up a row a frame ($38E9),
// and the rows fed in at the bottom are three more of the old level's bottom wall ($37D5), four more of the new level's
// top wall ($380C), and then the new level's 25 ($3839). The raster split ($20, the IRQ at $072E) gives the new level its
// colours from its first row down. The players hang where they were in dragons in bubbles ($E4DF), whose frame the IRQ
// turns every eight frames while the split is on ($0746), and glide to the new level's start points after (BubbleGlide).
internal sealed class LevelScroll
{
    internal const int Steps = GapRows + HeaderRows + PlayfieldRows;

    private const int PlayfieldRows = 25;
    private const int GapRows = 3;
    private const int HeaderRows = 4;
    private const int OutgoingRows = PlayfieldRows + GapRows;

    // A pass is two frames of the C64's.
    private const int RowsPerPass = 2;

    internal LevelScroll(PlayfieldModel outgoingField, SidebarModel outgoingSidebar, Level incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        OutgoingStart = outgoingField;
        OutgoingSidebarStart = outgoingSidebar;
        IncomingStart = Playfield.Build(incoming);

        // The new level's number is drawn when it starts ($3860 from $09DC), not on the way in.
        IncomingSidebarStart = Sidebars.Select(incoming) with { Number = 0 };
        Next = incoming.Number;
    }

    // The level the scroll brings in.
    internal int Next { get; }

    // How many rows the screen has moved up.
    internal int Rows { get; private set; }

    internal bool Finished => Rows == Steps;

    // The first screen row in the new level's colours: the split, which is under the screen until the first row of the
    // new level's top wall comes in.
    internal int Split => Math.Clamp(OutgoingRows - Rows, 0, PlayfieldRows);

    internal int BubbleFrame => BubbleGlide.BubbleFrame(Rows);

    internal PlayfieldModel OutgoingField => OutgoingStart.Scrolled(-Rows, 0, Split);

    internal SidebarModel OutgoingSidebar => OutgoingSidebarStart with { Shift = -Rows, EndRow = Split };

    internal PlayfieldModel IncomingField => IncomingStart.Scrolled(Steps - Rows, Split, PlayfieldRows);

    internal SidebarModel IncomingSidebar => IncomingSidebarStart with { Shift = Steps - Rows, FirstRow = Split };

    private PlayfieldModel OutgoingStart { get; }

    private SidebarModel OutgoingSidebarStart { get; }

    private PlayfieldModel IncomingStart { get; }

    private SidebarModel IncomingSidebarStart { get; }

    // $E4DF: the players in the game, in their dragons in bubbles where they were when the level ended.
    internal SpriteModel Sprites(EntityTable entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        return BubbleGlide.Bubbles(entities, entities.X, entities.Y, BubbleFrame);
    }

    internal void Pass() => Rows = Math.Min(Rows + RowsPerPass, Steps);
}
