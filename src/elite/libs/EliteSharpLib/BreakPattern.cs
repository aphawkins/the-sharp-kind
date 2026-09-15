// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Graphics;
using SharpKind;

namespace EliteSharpLib;

internal sealed class BreakPattern
{
    private const int MaxRings = 20;
    private readonly IEliteDraw _draw;
    private readonly FastColor _color;
    private float _breakPatternCount;

    internal BreakPattern(IEliteDraw draw)
    {
        _draw = draw;
        _color = _draw.Palette["White"];
    }

    internal bool IsComplete { get; private set; }

    // The rings have to fit a viewport that is wider than it is tall, so they
    // grow against its shorter half-extent rather than its width: the widest
    // ring drawn then reaches the top and bottom edges instead of running off
    // them. The innermost sits two steps out, the proportion the old bare 30
    // pixels had to the old spacing, so the pattern still opens on a ring
    // rather than a dot.
    // Update() resets the count at MaxRings, so the widest ring drawn is
    // index MaxRings - 2, which two steps out puts at exactly the half-extent.
    private float RingStep
        => MathF.Min(_draw.Layout.ViewportCentre.X, _draw.Layout.ViewportCentre.Y) / MaxRings;

    internal void Draw()
    {
        // Draw a break pattern (for launching, docking and hyperspacing).
        // Just draw a very simple one for the moment.
        float step = RingStep;

        for (int i = 0; i < (int)_breakPatternCount; i++)
        {
            _draw.Graphics.DrawCircle(_draw.Layout.ViewportCentre, (i + 2) * step, _color);
        }
    }

    internal void Reset()
    {
        _breakPatternCount = 0;
        IsComplete = false;
    }

    internal void Update(float ticks)
    {
        _breakPatternCount += ticks;

        if (_breakPatternCount >= MaxRings)
        {
            _breakPatternCount = 0;
            IsComplete = true;
        }
    }
}
