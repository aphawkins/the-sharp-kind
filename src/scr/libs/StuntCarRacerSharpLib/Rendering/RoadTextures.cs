// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using SharpKind;
using SharpKind.Graphics;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Rendering;

// Regenerated from the SCR palette: a road-line band at each edge with the road colour between.
// The original 400x100 bitmaps had every row identical and were only sampled at tv = 0, so a one-pixel-high strip is equivalent.
public sealed class RoadTextures
{
    internal const int YellowDark = 0;
    internal const int YellowLight = 1;
    internal const int RedDark = 2;
    internal const int RedLight = 3;
    internal const int Black = 4;
    internal const int White = 5;

    // The original bitmaps' proportions: 400 wide with a 9 pixel line on
    // the left and an 8 pixel line on the right.
    private const int Width = 400;
    private const int LeftLineWidth = 9;
    private const int RightLineWidth = 8;

    private readonly FastBitmap[] _textures;

    internal RoadTextures(ScrPalette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        _textures = new FastBitmap[6];
        _textures[0] = Strip(palette.Colour(Track.ScrBaseColour + 3), palette.Colour(Track.ScrBaseColour + 1));
        _textures[1] = Strip(palette.Colour(Track.ScrBaseColour + 3), palette.Colour(Track.ScrBaseColour + 2));
        _textures[2] = Strip(palette.Colour(Track.ScrBaseColour + 10), palette.Colour(Track.ScrBaseColour + 1));
        _textures[3] = Strip(palette.Colour(Track.ScrBaseColour + 10), palette.Colour(Track.ScrBaseColour + 2));
        _textures[4] = Strip(palette.Colour(Track.ScrBaseColour), palette.Colour(Track.ScrBaseColour));
        _textures[5] = Strip(palette.Colour(Track.ScrBaseColour + 15), palette.Colour(Track.ScrBaseColour + 15));
    }

    internal IReadOnlyList<FastBitmap> Textures => _textures;

    // Original SetSegmentTextures: line colour alternates yellow/red by segment, start line is white, black road stays black.
    internal static int[] SegmentTextures(Track track)
    {
        int[] segmentTextures = new int[track.NumSegments];
        int segment = 0;

        for (int pieceIndex = 0; pieceIndex < track.NumPieces; pieceIndex++)
        {
            TrackPiece piece = track.Pieces[pieceIndex];
            int roadLinesColour = piece.InitialColour;

            for (int s = 0; s < piece.NumSegments; s++)
            {
                bool startLine = pieceIndex == track.StartLinePiece && s == piece.NumSegments - 1;
                segmentTextures[segment++] = SegmentTexture(startLine, piece.RoadColours[s], roadLinesColour);

                // switch to the other road side lines colour
                roadLinesColour = roadLinesColour == 0 ? 1 : 0;
            }
        }

        return segmentTextures;
    }

    private static int SegmentTexture(bool startLine, byte roadColourIndex, int roadLinesColour)
        => startLine ? White : roadColourIndex switch
        {
            Track.ScrBaseColour + 1 => roadLinesColour == 0 ? YellowDark : RedDark,
            Track.ScrBaseColour + 2 => roadLinesColour == 0 ? YellowLight : RedLight,
            _ => Black,
        };

    private static FastBitmap Strip(in FastColor lineColour, in FastColor roadColour)
    {
        FastBitmap strip = new(Width, 1);
        for (int x = 0; x < Width; x++)
        {
            bool isLine = x is < LeftLineWidth or >= Width - RightLineWidth;
            strip.SetPixel(x, 0, isLine ? lineColour : roadColour);
        }

        return strip;
    }
}
