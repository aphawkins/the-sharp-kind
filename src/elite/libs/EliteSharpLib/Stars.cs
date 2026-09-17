// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views.Stars;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using EliteSharpLib.Views;
using SharpKind;

namespace EliteSharpLib;

internal sealed class Stars
{
    // Must be past the view's edge, not at it: both recycle paths put a star back at exactly
    // StarHalfHeight, so an edge of exactly that would recycle it forever. Kept as ratios (1.10x,
    // 1.16x the 8-bit half-height) rather than fixed numbers, since star space scales with focal length.
    private const float RearRecycleMargin = 1.10f;
    private const float SideRecycleMargin = 1.16f;

    private readonly IEliteDraw _draw;
    private readonly GameState _gameState;
    private readonly PlayerShip _ship;
    private readonly IStarfieldRenderer _renderer;

    // Off the draw surface, not the game's stream: taking these numbers from the game's stream
    // made other rolls depend on update count, so the same twenty seconds at two rates could meet
    // different ships. This is the coupling RenderRandom removed from the drawing.
    private readonly IRandomSource _rng;

    // What this frame is showing, refilled by each starfield pass and handed
    // to the rendition to draw. Which stars go in is the game's decision; how
    // one looks is not.
    private readonly List<StarMark> _marks = [];

    private Vector4[] _stars = [];

    internal Stars(GameState gameState, IEliteDraw draw, PlayerShip ship, IStarfieldRenderer renderer)
    {
        _rng = draw.Jitter;
        _gameState = gameState;
        _ship = ship;
        _draw = draw;
        _renderer = renderer;
    }

    internal bool WarpStars { get; set; }

    // How many stars are currently simulated and drawn. Tracks _stars' own
    // length rather than a separate field, so the two can never desync.
    private int Count => _stars.Length;

    // Star coordinates are in the original's 256-wide space, mapping to the screen by the same
    // factor as projected planet/sun radii, so the starfield exactly fills the view at any tier.
    private float StarScale => _draw.Focus / 256;

    private float StarHalfWidth => _draw.Layout.ViewportCentre.X / StarScale;

    private float StarHalfHeight => _draw.Layout.ViewportCentre.Y / StarScale;

    /// <summary>
    /// Hands this tick's starfield to the rendition, then empties it.
    /// </summary>
    /// <remarks>
    /// Separate from the four starfield passes, which now only move the
    /// stars and collect what to show: the marks are worked out while the
    /// game ticks and drawn while the frame is composed.
    /// <para>
    /// Emptying as it draws is what makes it safe to call every frame. A
    /// screen with no starfield never fills the list, so it draws nothing,
    /// rather than repainting whatever the last cockpit view left behind.
    /// </para>
    /// </remarks>
    internal void Draw()
    {
        _renderer.Draw(_marks);
        _marks.Clear();
    }

    /// <summary>
    /// Refills normal space with the rendition's own <see
    /// cref="IStarfieldRenderer.NormalSpaceStarCount"/>.
    /// </summary>
    internal void CreateNewStars() => CreateNewStars(_renderer.NormalSpaceStarCount);

    /// <summary>
    /// Refills witchspace with the rendition's own <see
    /// cref="IStarfieldRenderer.WitchspaceStarCount"/>, visibly emptier than
    /// normal space.
    /// </summary>
    internal void CreateNewWitchspaceStars() => CreateNewStars(_renderer.WitchspaceStarCount);

    /// <summary>
    /// When we change view, flip the stars over so they look like other stars.
    /// </summary>
    internal void FlipStars()
    {
        for (int i = 0; i < Count; i++)
        {
            _stars[i].X = -_stars[i].X;
            _stars[i].Y = -_stars[i].Y;
        }
    }

    internal void FrontStarfield()
    {
        _marks.Clear();
        float ticks = _gameState.Clock.Ticks;
        float delta = (WarpStars ? 50 : _ship.Speed) * ticks;
        float alpha = _ship.Roll * ticks;
        float beta = _ship.Pitch * ticks;
        float gamma = _ship.Yaw * ticks;

        alpha /= 256;
        delta /= 2;

        for (int i = 0; i < Count; i++)
        {
            Vector2 star = PlotStar(i);

            // Move the stars to their new locations...
            float q = delta / _stars[i].Z;

            _stars[i].Z -= delta;
            float yy = _stars[i].Y + (_stars[i].Y * q);
            float xx = _stars[i].X + (_stars[i].X * q);
            float zz = _stars[i].Z;

            yy += xx * alpha;
            xx -= yy * alpha;

            ////tx = yy * beta;
            ////xx = xx + (tx * tx * 2);
            yy += beta;

            // Yaw pans the view sideways the way pitch pans it up and down.
            xx -= gamma;

            _stars[i].Y = yy;
            _stars[i].X = xx;

            if (WarpStars)
            {
                // Not checked against the view: the renderer clips whatever streak falls outside.
                _marks.Add(new(star, ToScreen(xx, yy), true, zz));
            }

            star.X = xx;
            star.Y = yy;

            if ((star.X > StarHalfWidth)
                || (star.X < -StarHalfWidth) ||
                (star.Y > (_draw.Layout.ViewportBottom - _draw.Layout.ViewportCentre.Y) / StarScale)
                || (star.Y < -StarHalfHeight) ||
                (zz < 16))
            {
                _stars[i] = CreateNewStar();
            }
        }

        WarpStars = false;
    }

    internal void LeftStarfield()
    {
        float ticks = _gameState.Clock.Ticks;
        float delta = (WarpStars ? 50 : _ship.Speed) * ticks;
        SideStarfield(-_ship.Roll * ticks, -_ship.Pitch * ticks, _ship.Yaw * ticks, -delta);
    }

    internal void RearStarfield()
    {
        _marks.Clear();
        float ticks = _gameState.Clock.Ticks;
        float delta = (WarpStars ? 50 : _ship.Speed) * ticks;
        float alpha = -_ship.Roll * ticks;
        float beta = -_ship.Pitch * ticks;
        float gamma = -_ship.Yaw * ticks;

        alpha /= 256;
        delta /= 2;

        for (int i = 0; i < Count; i++)
        {
            Vector2 star = PlotStar(i);

            // Move the stars to their new locations...
            float q = delta / _stars[i].Z;

            _stars[i].Z += delta;
            float yy = _stars[i].Y - (_stars[i].Y * q);
            float xx = _stars[i].X - (_stars[i].X * q);
            float zz = _stars[i].Z;

            yy += xx * alpha;
            xx -= yy * alpha;

            ////tx = yy * beta;
            ////xx = xx + (tx * tx * 2);
            yy += beta;

            // Looking aft, the pan is the other way about - the same reason
            // the roll and the pitch are negated above.
            xx -= gamma;

            if (WarpStars)
            {
                DrawStarStreak(star, xx, yy);
            }

            _stars[i].Y = yy;
            _stars[i].X = xx;

            if ((zz >= 300) || (MathF.Abs(yy) >= (StarHalfHeight * RearRecycleMargin)))
            {
                RecycleStarAtEdge(i);
            }
        }

        WarpStars = false;
    }

    internal void RightStarfield()
    {
        float ticks = _gameState.Clock.Ticks;
        float delta = (WarpStars ? 50 : _ship.Speed) * ticks;
        SideStarfield(_ship.Roll * ticks, _ship.Pitch * ticks, _ship.Yaw * ticks, delta);
    }

    // Star space (centred on the view) to screen pixels.
    private Vector2 ToScreen(float xx, float yy)
        => _draw.Layout.ViewportCentre + (new Vector2(xx, yy) * StarScale);

    // Draw the motion streak from a star's old screen position to where it has
    // just moved to, when both ends are inside the view.
    private void DrawStarStreak(Vector2 star, float xx, float yy)
    {
        Vector2 end = ToScreen(xx, yy);
        float ex = end.X;
        float ey = end.Y;

        if ((star.X >= _draw.Layout.ViewportLeft)
            && (star.X <= _draw.Layout.ViewportRight) &&
            (star.Y >= _draw.Layout.ViewportTop)
            && (star.Y <= _draw.Layout.ViewportBottom) &&
            (ex >= _draw.Layout.ViewportLeft)
            && (ex <= _draw.Layout.ViewportRight) &&
            (ey >= _draw.Layout.ViewportTop)
            && (ey <= _draw.Layout.ViewportBottom))
        {
            _marks.Add(new(star, new(ex, ey), true, 0));
        }
    }

    // A star that has passed the camera or run off the top or bottom comes
    // back in at a random point on one of the view's edges.
    private void RecycleStarAtEdge(int i)
    {
        _stars[i].Z = _rng.Random(51, 179);

        if (_rng.TrueOrFalse())
        {
            _stars[i].X = _rng.Random(-(int)StarHalfWidth, (int)StarHalfWidth);
            _stars[i].Y = _rng.TrueOrFalse() ? -(int)StarHalfHeight : (int)StarHalfHeight;
        }
        else
        {
            _stars[i].X = _rng.TrueOrFalse() ? -(int)StarHalfWidth : (int)StarHalfWidth;
            _stars[i].Y = _rng.Random(-(int)StarHalfHeight, (int)StarHalfHeight);
        }
    }

    private Vector4 CreateNewStar() => new()
    {
        X = _rng.Random(-(int)StarHalfWidth, (int)StarHalfWidth) | 8,
        Y = _rng.Random(-(int)StarHalfHeight, (int)StarHalfHeight) | 4,
        Z = _rng.Random(256) | 144,
    };

    private void CreateNewStars(int count)
    {
        _stars = new Vector4[Math.Max(1, count)];
        for (int i = 0; i < Count; i++)
        {
            _stars[i] = CreateNewStar();
        }

        WarpStars = false;
    }

    // Returns the screen position so the caller can draw a motion streak from it once the star has moved.
    private Vector2 PlotStar(int i)
    {
        Vector2 star = new()
        {
            Y = _stars[i].Y,
            X = _stars[i].X,
        };
        float zz = _stars[i].Z;

        star = ToScreen(star.X, star.Y);

        if ((!WarpStars) &&
            (star.X >= _draw.Layout.ViewportLeft)
            && (star.X <= _draw.Layout.ViewportRight) &&
            (star.Y >= _draw.Layout.ViewportTop)
            && (star.Y <= _draw.Layout.ViewportBottom))
        {
            _marks.Add(new(star, star, false, zz));
        }

        return star;
    }

    private void SideStarfield(float alpha, float beta, float gamma, float delta)
    {
        _marks.Clear();
        for (int i = 0; i < Count; i++)
        {
            Vector2 star = PlotStar(i);

            float yy = _stars[i].Y;
            float xx = _stars[i].X;
            float zz = _stars[i].Z;

            float delt8 = delta / (zz / 32);
            xx += delt8;

            xx += yy * (beta / 256);
            yy -= xx * (beta / 256);

            xx += yy / 256 * (alpha / 256) * (-xx);
            yy += yy / 256 * (alpha / 256) * yy;

            yy += alpha;

            // Yaw swings the nose about the axis a side view looks across,
            // so it pans that view sideways, both views the same way.
            xx += gamma;

            _stars[i].Y = yy;
            _stars[i].X = xx;

            if (WarpStars)
            {
                _marks.Add(new(star, ToScreen(xx, yy), true, zz));
            }

            if (MathF.Abs(_stars[i].X) >= StarHalfWidth)
            {
                _stars[i].X = (_gameState.CurrentScreen == Screen.LeftView) ? StarHalfWidth : -StarHalfWidth;
                _stars[i].Y = _rng.Random(-(int)StarHalfHeight, (int)StarHalfHeight);
                _stars[i].Z = _rng.Random(256) | 8;
            }
            else if (MathF.Abs(_stars[i].Y) >= (StarHalfHeight * SideRecycleMargin))
            {
                _stars[i].X = _rng.Random(-(int)StarHalfWidth, (int)StarHalfWidth);
                _stars[i].Y = (alpha > 0) ? -StarHalfHeight : StarHalfHeight;
                _stars[i].Z = _rng.Random(256) | 8;
            }
        }

        WarpStars = false;
    }
}
