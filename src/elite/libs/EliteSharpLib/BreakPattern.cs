// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
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

    // Grows to the distance from the centre to the bottom-right pixel, so the
    // widest ring passes through the viewport's corners and the window clip
    // trims the rest.
    private float RingStep
        => Vector2.Distance(_draw.Layout.ViewportCentre, new(_draw.Layout.ViewportRight, _draw.Layout.ViewportBottom))
            / MaxRings;

    internal void Draw()
    {
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
