// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

using System.Numerics;
using SharpKind;
using SharpKind.Graphics;
using StuntCarRacerSharpLib.Tracks;

namespace StuntCarRacerSharpLib.Rendering;

// Depth tested per pixel to match the original's D3DRS_ZENABLE z-buffer, so draw order is arbitrary.
public sealed class TrackRenderer
{
    private const int MaxPolygonPoints = 5; // quad plus one clip point

    // Road segments this far from the player draw road lines (original TEXTURED_SEGMENTS_AROUND_PLAYER).
    private const int TexturedSegmentsAroundPlayer = 11;

    // u = 0 left edge, 1 right edge (tv always 0), matching original StorePieceTriangle; order matches the road quad below.
    private static readonly Vector2[] s_roadTextureCoords =
        [new(0, 0), new(0, 0), new(1, 0), new(1, 0)];

    private readonly Track _track;

    private readonly IGraphics _graphics;

    private readonly ScreenLayout _screen;

    private readonly ScrPalette _palette;

    private readonly Scene3D _scene = new();

    private readonly RoadTextures _roadTextures;

    private readonly int[] _segmentTextures;

    public TrackRenderer(Track track, IGraphics graphics, ScreenLayout screen, ScrPalette palette, RoadTextures roadTextures)
    {
        ArgumentNullException.ThrowIfNull(track);
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(palette);
        ArgumentNullException.ThrowIfNull(roadTextures);

        _track = track;
        _graphics = graphics;
        _screen = screen;
        _palette = palette;
        _roadTextures = roadTextures;
        _segmentTextures = RoadTextures.SegmentTextures(track);
    }

    public void Draw(SceneCamera camera) => Draw(camera, null);

    public void Draw(SceneCamera camera, IEnumerable<WorldPolygon>? extraPolygons)
        => Draw(camera, extraPolygons, -1, 0);

    // Road draws after side walls so it wins shared edges (original used a LESSEQUAL z test); playerPiece -1 disables road lines.
    public void Draw(SceneCamera camera, IEnumerable<WorldPolygon>? extraPolygons, int playerPiece, int playerSegment)
    {
        _scene.SetView(camera, _screen.ScreenWidth, _screen.ScreenHeight);
        _graphics.ClearDepth();

        int playerGlobalSegment = playerPiece < 0
            ? -1
            : _track.Pieces[playerPiece].FirstSegment + playerSegment;

        if (extraPolygons != null)
        {
            foreach (WorldPolygon polygon in extraPolygons)
            {
                DrawWorldPolygon(polygon.Points, polygon.Colour, null, default);
            }
        }

        Span<Coord3D> world = stackalloc Coord3D[4];
        for (int pieceIndex = 0; pieceIndex < _track.NumPieces; pieceIndex++)
        {
            TrackPiece piece = _track.Pieces[pieceIndex];

            // piece's front left corner within the world, in track units
            int pieceX = piece.CubeX << (Track.CubeSizeLog2 - Track.LogPrecision);
            int pieceY = piece.CubeY << (Track.CubeSizeLog2 - Track.LogPrecision);
            int pieceZ = piece.CubeZ << (Track.CubeSizeLog2 - Track.LogPrecision);

            for (int segment = 0; segment < piece.NumSegments; segment++)
            {
                int offset = segment * 4;

                // left side: top left, bottom left, next bottom left, next top left (original triangles 0,2,6 and 0,6,4)
                FastColor sideColour = _palette.Colour(piece.SidesColour);
                world[0] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset);
                world[1] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 2);
                world[2] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 6);
                world[3] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 4);
                DrawWorldPolygon(world, sideColour, null, default);

                // right side: next top right, next bottom right, bottom right, top right (original triangles 5,7,3 and 5,3,1)
                world[0] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 5);
                world[1] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 7);
                world[2] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 3);
                world[3] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 1);
                DrawWorldPolygon(world, sideColour, null, default);

                // road surface: top left, next top left, next top right, top right (original triangles 0,4,5 and 0,5,1)
                byte roadColour = piece.RoadColours[segment];
                if (pieceIndex == _track.StartLinePiece && segment == piece.NumSegments - 1)
                {
                    // set colour to white for the start line
                    roadColour = Track.ScrBaseColour + 15;
                }

                world[0] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset);
                world[1] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 4);
                world[2] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 5);
                world[3] = PieceVertex(piece, pieceX, pieceY, pieceZ, offset + 1);

                int globalSegment = piece.FirstSegment + segment;
                if (IsRoadLined(globalSegment, playerGlobalSegment))
                {
                    DrawWorldPolygon(
                        world,
                        _palette.Colour(roadColour),
                        _roadTextures.Textures[_segmentTextures[globalSegment]],
                        s_roadTextureCoords);
                }
                else
                {
                    DrawWorldPolygon(world, _palette.Colour(roadColour), null, default);
                }
            }
        }
    }

    // y divided by 4 for display, as original GetPieceVertex.
    private static Coord3D PieceVertex(TrackPiece piece, int pieceX, int pieceY, int pieceZ, int offset)
    {
        Coord3D coord = piece.Coords[offset];
        return new(coord.X + pieceX, (coord.Y / 4) + pieceY, coord.Z + pieceZ);
    }

    // Wraps around the track when measuring distance to the player.
    private bool IsRoadLined(int globalSegment, int playerGlobalSegment)
    {
        if (playerGlobalSegment < 0)
        {
            return false;
        }

        int distance = Math.Abs(globalSegment - playerGlobalSegment);
        distance = Math.Min(distance, _track.NumSegments - distance);
        return distance <= TexturedSegmentsAroundPlayer;
    }

    // Clipped one triangle at a time: clipping a twisted quad's outline directly can self-intersect, but a triangle is always convex.
    private void DrawWorldPolygon(
        in ReadOnlySpan<Coord3D> world,
        in FastColor colour,
        FastBitmap? texture,
        in ReadOnlySpan<Vector2> textureCoords)
    {
        Span<Vector3> cameraSpace = stackalloc Vector3[MaxPolygonPoints - 1];
        cameraSpace = cameraSpace[..world.Length];
        for (int i = 0; i < world.Length; i++)
        {
            Coord3D transformed = _scene.TransformPoint(world[i].X, world[i].Y, world[i].Z);
            cameraSpace[i] = new(transformed.X, transformed.Y, transformed.Z);
        }

        Span<Vector3> triangle = stackalloc Vector3[3];
        Span<Vector2> triangleUv = stackalloc Vector2[3];
        Span<Vector3> clipped = stackalloc Vector3[4];
        Span<Vector2> clippedUv = stackalloc Vector2[4];
        for (int i = 1; i < cameraSpace.Length - 1; i++)
        {
            triangle[0] = cameraSpace[0];
            triangle[1] = cameraSpace[i];
            triangle[2] = cameraSpace[i + 1];

            int count;
            if (texture == null)
            {
                count = NearPlaneClip.Clip(triangle, Scene3D.NearPlane, clipped);
            }
            else
            {
                triangleUv[0] = textureCoords[0];
                triangleUv[1] = textureCoords[i];
                triangleUv[2] = textureCoords[i + 1];
                count = NearPlaneClip.Clip(triangle, triangleUv, Scene3D.NearPlane, clipped, clippedUv);
            }

            if (count < 3)
            {
                continue; // fully behind the near plane
            }

            // emitted as triangles too, so rounding on a long thin quad can't produce a concave fan fill
            for (int j = 1; j < count - 1; j++)
            {
                Vector2[] points =
                [
                    _scene.ProjectPoint(clipped[0]),
                    _scene.ProjectPoint(clipped[j]),
                    _scene.ProjectPoint(clipped[j + 1]),
                ];
                float[] depths = [clipped[0].Z, clipped[j].Z, clipped[j + 1].Z];

                if (texture != null)
                {
                    _graphics.DrawPolygonTexturedDepth(
                        points,
                        depths,
                        [clippedUv[0], clippedUv[j], clippedUv[j + 1]],
                        texture);
                }
                else
                {
                    _graphics.DrawPolygonFilledDepth(points, depths, colour);
                }
            }
        }
    }
}
