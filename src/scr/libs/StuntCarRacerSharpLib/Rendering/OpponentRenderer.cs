// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using StuntCarRacerSharpLib.Cars;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Rendering;

// Draws the opponent using the Car.cpp mesh oriented on its wheel
// positions, plus its road shadow.
public sealed class OpponentRenderer
{
    private const int ShadowColour = Track.ScrBaseColour + 5;

    // The Amiga had no transparency, so its shadow was a solid fill. Drawn translucent instead, so road markings stay visible through it.
    private const byte ShadowAlpha = 160;

    private readonly OpponentPhysics _opponent;
    private readonly CarMesh _carMesh;
    private readonly ScrPalette _palette;

    public OpponentRenderer(OpponentPhysics opponent, CarMesh carMesh, ScrPalette palette)
    {
        ArgumentNullException.ThrowIfNull(opponent);
        ArgumentNullException.ThrowIfNull(carMesh);
        ArgumentNullException.ThrowIfNull(palette);
        _opponent = opponent;
        _carMesh = carMesh;
        _palette = palette;
    }

    // Appends the opponent's polygons (world track units) for depth-sorted
    // drawing with the track.
    public void AppendWorldPolygons(ICollection<WorldPolygon> polygons)
    {
        ArgumentNullException.ThrowIfNull(polygons);

        if (_opponent.OpponentId < 0)
        {
            return;
        }

        // shadow quad, slightly above the road
        if (_opponent.ShadowVisible)
        {
            FastColor shadow = _palette.Colour(ShadowColour);
            polygons.Add(new(
                [_opponent.ShadowRearLeft, _opponent.ShadowRearRight, _opponent.ShadowFrontRight, _opponent.ShadowFrontLeft],
                new FastColor(ShadowAlpha, shadow.R, shadow.G, shadow.B)));
        }

        _carMesh.Append(
            polygons,
            _opponent.VisualRearLeft,
            _opponent.VisualRearRight,
            _opponent.VisualFrontLeft,
            _opponent.VisualFrontRight);
    }
}
