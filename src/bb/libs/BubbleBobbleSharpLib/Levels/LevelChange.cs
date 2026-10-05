// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

using BubbleBobbleSharp.Abstractions.Views;
using BubbleBobbleSharpLib.Players;

namespace BubbleBobbleSharpLib.Levels;

// The way from one level to another, while nothing else moves: the scroll to the next ($37C9), and for an item that skips
// levels ($7FB2) the six flashes first and a scroll through every level on the way ($7FD4-$7FEE); and last the players'
// bubbles gliding to the new level's start points ($E554).
internal sealed class LevelChange(LevelStore levels)
{
    // $7FB2-$7FD1: the border and background flash red ($00 EOR $02) six times, ten frames apart.
    private const int FlashUpdates = 30;
    private const int UpdatesPerFlash = 5;

    private int _to;
    private int _flash;

    internal LevelScroll? Scroll { get; private set; }

    internal bool Active => _flash > 0 || Scroll is not null || Glide is not null;

    // The new level has started, and its items, bubbles and effects are not drawn until the glide is over.
    internal bool Gliding => Glide is not null;

    // Nothing but the levels and the players' bubbles is drawn.
    internal bool InTransit => Scrolling || Gliding;

    internal bool Scrolling => Scroll is not null;

    internal BubbleGlide? Glide { get; private set; }

    // How many rows the old level has gone up the screen.
    internal int Rows => Scroll?.Rows ?? 0;

    // Each flash turns the screen red and the next turns it back, so the first, third and fifth show.
    internal bool FlashShows
    {
        get
        {
            int flashes = (FlashUpdates - _flash) / UpdatesPerFlash;

            return _flash > 0 && flashes % 2 == 0;
        }
    }

    // The level as drawn: the old over the new while the screen scrolls.
    internal PlayfieldModel[] Fields(PlayfieldModel current)
        => Scroll is LevelScroll scroll ? [scroll.OutgoingField, scroll.IncomingField] : [current];

    internal SidebarModel[] Sidebars(SidebarModel current)
        => Scroll is LevelScroll scroll ? [scroll.OutgoingSidebar, scroll.IncomingSidebar] : [current];

    // The players in their bubbles while the screen scrolls, or none.
    internal SpriteModel? Sprites(EntityTable entities) => Scroll?.Sprites(entities) ?? Glide?.Sprites(entities);

    // $0A6A: a cleared level scrolls on to the next.
    internal void ScrollTo(PlayfieldModel field, SidebarModel sidebar, int current, int to)
    {
        _to = to;
        Scroll = new LevelScroll(field, sidebar, levels.Level(current + 1));
    }

    // $7FB2: an item has skipped levels.
    internal void SkipTo(int to)
    {
        _to = to;
        _flash = FlashUpdates;
    }

    // Returns the level to start once the way there is over, or none.
    internal int? Pass(PlayfieldModel field, SidebarModel sidebar, int current, EntityTable entities)
    {
        if (Glide is BubbleGlide glide)
        {
            glide.Pass();

            if (glide.Finished(entities))
            {
                Glide = null;
            }

            return null;
        }

        if (_flash > 0)
        {
            _flash--;

            if (_flash == 0)
            {
                ScrollTo(field, sidebar, current, _to);
            }

            return null;
        }

        if (Scroll is not LevelScroll scroll)
        {
            return null;
        }

        scroll.Pass();

        if (!scroll.Finished)
        {
            return null;
        }

        Scroll = null;

        if (scroll.Next < _to)
        {
            // $7FD4-$7FEE: a level skipped is drawn ($E000) and scrolled past ($37C9) on the way to the next.
            Level passed = levels.Level(scroll.Next);

            Scroll = new LevelScroll(Playfield.Build(passed), Levels.Sidebars.Select(passed), levels.Level(scroll.Next + 1));
            return null;
        }

        // $09DC-$09E8: the level is set up, its tune started, and then the players glide to it.
        Glide = new BubbleGlide(entities, LevelScroll.Steps);

        return scroll.Next;
    }
}
