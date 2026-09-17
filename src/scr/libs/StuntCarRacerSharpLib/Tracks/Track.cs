// 'Stunt Car Racer - The Sharp Kind' - Andy Hawkins 2026.
// 'Stunt Car Racer Remake' - sourceforge.net/projects/stuntcarremake.
// Stunt Car Racer (C) Geoff Crammond / MicroStyle / MicroProse 1989.

namespace StuntCarRacerSharpLib.Tracks;

// Loads the original Amiga track data (804 byte .bin files) and converts it into world-space track pieces, as Track.cpp does.
public sealed class Track
{
    // All Amiga track data is made 2 times bigger for use by PC StuntCarRacerSharp.
    internal const int PcFactor = 2;

    internal const int ScrBaseColour = 26;

    // From the original 3D Engine.h fixed-point conventions.
    internal const int MaxAngle = 65536;

    internal const int LogPrecision = 14;

    internal const int MaxPieces = 100;

    internal const int MaxSegments = 13;

    // x,z dimensions of the track map, in cubes.
    internal const int TrackCubes = 16;

    internal const int CubeSizeLog2 = 26;

    // Size of a single track cube (0x800 * PcFactor of 2 * PRECISION).
    internal const int CubeSize = 1 << CubeSizeLog2;

    internal const int BottomY = 0;

    private const int TrackDataSize = 804;

    private const int MaxYCoordsPerPiece = MaxSegments + 1;

    // Flags for each near section; if bit 7 is set the car cannot be lowered onto it.
    private static readonly byte[] s_sectionsCarCanBePutOn =
    [
        0x00, 0x80, 0x20, 0xc0, 0x00, 0x73, 0x80, 0xc0,
        0xa9, 0x59, 0x00, 0x02, 0xa9, 0x5e, 0x85, 0x4b,
    ];

    // Piece templates - only indices 0, 1, 3, 4, 6, 7, 10 are used.
    private static readonly PieceTemplate?[] s_pieceTemplates =
    [
        new(8, false, 0x00, 128, 0x20, AmigaTrackData.Piece0XZ),
        new(8, false, 0x80, 135, 0x3e, AmigaTrackData.Piece1XZ),
        null,
        new(8, true, 0xc0, 135, 0x3e, AmigaTrackData.Piece3XZ),
        new(13, false, 0x40, 128, 0x20, AmigaTrackData.Piece4XZ),
        null,
        new(9, false, 0x80, 122, 0x32, AmigaTrackData.Piece6XZ),
        new(9, true, 0xc0, 122, 0x32, AmigaTrackData.Piece7XZ),
        null,
        null,
        new(11, false, 0x40, 124, 0x20, AmigaTrackData.Piece10XZ),
    ];

    // Converted piece template coordinates, built once from the Amiga data.
    private static readonly Lazy<ConvertedTemplates> s_converted = new(ConvertAmigaPieceData);

    private readonly int[] _map = new int[TrackCubes * TrackCubes];

    private readonly TrackPiece[] _pieces;

    private Track(TrackId id, byte[] buffer)
    {
        Id = id;

        int i = 0;
        int numPieces = buffer[i++];
        PlayersStartPiece = buffer[i++];
        StartLinePiece = PlayersStartPiece;

        HalfALapPiece = StartLinePiece + (numPieces / 2);
        if (HalfALapPiece > numPieces)
        {
            HalfALapPiece -= numPieces;
        }

        byte[] piecePositions = new byte[MaxPieces];
        byte[] angleAndTemplate = new byte[MaxPieces];
        byte[] leftYId = new byte[MaxPieces];
        byte[] rightYId = new byte[MaxPieces];
        short[] leftYShift = new short[MaxPieces];
        short[] rightYShift = new short[MaxPieces];

        Array.Copy(buffer, i, piecePositions, 0, MaxPieces);
        i += MaxPieces;
        Array.Copy(buffer, i, angleAndTemplate, 0, MaxPieces);
        i += MaxPieces;
        Array.Copy(buffer, i, leftYId, 0, MaxPieces);
        i += MaxPieces;
        Array.Copy(buffer, i, rightYId, 0, MaxPieces);
        i += MaxPieces;

        for (int j = 0; j < MaxPieces; j++)
        {
            leftYShift[j] = (short)((buffer[i] << 8) | buffer[i + 1]);
            i += 2;
        }

        for (int j = 0; j < MaxPieces; j++)
        {
            rightYShift[j] = (short)((buffer[i] << 8) | buffer[i + 1]);
            i += 2;
        }

        StandardBoost = buffer[i++];
        SuperBoost = buffer[i];

        _pieces = ConvertAmigaTrack(numPieces, piecePositions, angleAndTemplate, leftYId, rightYId, leftYShift, rightYShift);
        NumSegments = _pieces.Length == 0 ? 0 : _pieces[^1].FirstSegment + _pieces[^1].NumSegments;
    }

    public TrackId Id { get; }

    public string Name => Id switch
    {
        TrackId.LittleRamp => "Little Ramp",
        TrackId.SteppingStones => "Stepping Stones",
        TrackId.HumpBack => "Hump Back",
        TrackId.BigRamp => "Big Ramp",
        TrackId.SkiJump => "Ski Jump",
        TrackId.DrawBridge => "Draw Bridge",
        TrackId.HighJump => "High Jump",
        TrackId.RollerCoaster => "Roller Coaster",
        _ => string.Empty,
    };

    public IReadOnlyList<TrackPiece> Pieces => _pieces;

    public int NumPieces => _pieces.Length;

    public int NumSegments { get; }

    public int PlayersStartPiece { get; }

    public int StartLinePiece { get; }

    public int HalfALapPiece { get; }

    public int StandardBoost { get; }

    public int SuperBoost { get; }

    public static Track Load(TrackId id, Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] buffer = new byte[TrackDataSize];
        stream.ReadExactly(buffer);
        return new(id, buffer);
    }

    public static Track Load(TrackId id)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Tracks", $"{id}.bin");
        using FileStream stream = File.OpenRead(path);
        return Load(id, stream);
    }

    // The piece occupying a track map cube, or -1 for empty cubes.
    public int GetMapPiece(int cubeX, int cubeZ) => _map[(cubeX * TrackCubes) + cubeZ];

    // If bit 7 is set then the car cannot be lowered onto this near section.
    internal static byte SectionCarCanBePutOn(int nearSection) => s_sectionsCarCanBePutOn[nearSection];

    private static ConvertedTemplates ConvertAmigaPieceData()
    {
        (int X, int Z)[]?[] xz = new (int X, int Z)[]?[s_pieceTemplates.Length];
        for (int i = 0; i < s_pieceTemplates.Length; i++)
        {
            PieceTemplate? template = s_pieceTemplates[i];
            if (template != null)
            {
                xz[i] = ConvertAmigaPieceXZ(template.AmigaXZ);
            }
        }

        int[][] y = new int[AmigaTrackData.PieceYTemplates.Length][];
        for (int i = 0; i < AmigaTrackData.PieceYTemplates.Length; i++)
        {
            AmigaPieceY? amiga = AmigaTrackData.PieceYTemplates[i];
            y[i] = amiga == null ? new int[MaxYCoordsPerPiece] : ConvertAmigaPieceY(amiga);
        }

        return new(xz, y);
    }

    // Amiga x,z data is little-endian signed 16-bit pairs.
    private static (int X, int Z)[] ConvertAmigaPieceXZ(byte[] amiga)
    {
        (int X, int Z)[] dest = new (int X, int Z)[amiga.Length / 4];
        for (int i = 0; i < dest.Length; i++)
        {
            short x = (short)((amiga[(i * 4) + 1] << 8) | amiga[i * 4]);
            short z = (short)((amiga[(i * 4) + 3] << 8) | amiga[(i * 4) + 2]);
            dest[i] = (x, z);
        }

        return dest;
    }

    private static int[] ConvertAmigaPieceY(AmigaPieceY amiga)
    {
        int[] dest = new int[MaxYCoordsPerPiece];
        if (amiga.Words)
        {
            // 15-bit big-endian words.
            int number = amiga.Data.Length / 2;
            for (int i = 0; i < number; i++)
            {
                dest[i] = ((amiga.Data[i * 2] & 0x7f) << 8) | amiga.Data[(i * 2) + 1];
            }
        }
        else
        {
            // Packed bytes: high nibble holds bits 5-7, low nibble holds bits 8-11.
            for (int i = 0; i < amiga.Data.Length; i++)
            {
                int b = amiga.Data[i];
                int ya = (b << 1) & 0xe0;
                int yb = (b & 0x0f) << 8;
                dest[i] = ya | yb;
            }
        }

        return dest;
    }

    private static (int X, int Z) GetRotatedPieceXZ((int X, int Z) coord, int roughAngle) => roughAngle switch
    {
        0x40 => (coord.Z, 0x800 - coord.X), // 90 degrees clockwise
        0x80 => (0x800 - coord.X, 0x800 - coord.Z), // 180 degrees
        0xc0 => (0x800 - coord.Z, coord.X), // 270 degrees clockwise
        _ => (coord.X, coord.Z), // 0 degrees
    };

    private static void StorePieceXZ(
        TrackPiece trackPiece,
        (int X, int Z)[] pieceXZ,
        int roughAngle,
        int numCoords,
        bool reverseOrder)
    {
        int j = reverseOrder ? (numCoords * 2) - 1 : 0;
        int step = reverseOrder ? -1 : 1;

        for (int i = 0; i < numCoords; i++)
        {
            (int x, int z) = GetRotatedPieceXZ(pieceXZ[j], roughAngle);
            x *= PcFactor;
            z *= PcFactor;

            // Top left and bottom left.
            trackPiece.Coords[i * 4] = new(x, 0, z);
            trackPiece.Coords[(i * 4) + 2] = new(x, 0, z);
            j += step;

            (x, z) = GetRotatedPieceXZ(pieceXZ[j], roughAngle);
            x *= PcFactor;
            z *= PcFactor;

            // Top right and bottom right.
            trackPiece.Coords[(i * 4) + 1] = new(x, 0, z);
            trackPiece.Coords[(i * 4) + 3] = new(x, 0, z);
            j += step;
        }
    }

    private static void StorePieceY(
        TrackPiece trackPiece,
        int[] leftY,
        int[] rightY,
        int leftYShift,
        int rightYShift,
        int numCoords)
    {
        for (int i = 0; i < numCoords; i++)
        {
            Coord3D topLeft = trackPiece.Coords[i * 4];
            Coord3D topRight = trackPiece.Coords[(i * 4) + 1];
            trackPiece.Coords[i * 4] = topLeft with { Y = (leftY[i] + leftYShift) * PcFactor };
            trackPiece.Coords[(i * 4) + 1] = topRight with { Y = (rightY[i] + rightYShift) * PcFactor };
            trackPiece.Coords[(i * 4) + 2] = trackPiece.Coords[(i * 4) + 2] with { Y = BottomY };
            trackPiece.Coords[(i * 4) + 3] = trackPiece.Coords[(i * 4) + 3] with { Y = BottomY };
        }
    }

    // Set road surface colours; segments with a large y step are black.
    private static void SetRoadColours(TrackPiece trackPiece, int numSegments, byte roadColour)
    {
        for (int i = 0; i < numSegments; i++)
        {
            int y1 = trackPiece.Coords[i * 4].Y - trackPiece.Coords[(i + 1) * 4].Y;
            int y2 = trackPiece.Coords[(i * 4) + 1].Y - trackPiece.Coords[((i + 1) * 4) + 1].Y;

            int y = Math.Abs(y1) > Math.Abs(y2) ? y1 : y2;

            trackPiece.RoadColours[i] = Math.Abs(y) >= 640 * PcFactor ? (byte)ScrBaseColour : roadColour;
        }
    }

    // Copies each piece's start coordinates to the previous piece's end coordinates, adjusted for cube position, so pieces join exactly.
    private static void JoinPieces(TrackPiece[] pieces, int numPieces)
    {
        for (int piece = 0; piece < numPieces; piece++)
        {
            int lastPiece = piece > 0 ? piece - 1 : numPieces - 1;

            int cubeX = pieces[piece].CubeX << (CubeSizeLog2 - LogPrecision);
            int cubeY = pieces[piece].CubeY << (CubeSizeLog2 - LogPrecision);
            int cubeZ = pieces[piece].CubeZ << (CubeSizeLog2 - LogPrecision);

            int lastCubeX = pieces[lastPiece].CubeX << (CubeSizeLog2 - LogPrecision);
            int lastCubeY = pieces[lastPiece].CubeY << (CubeSizeLog2 - LogPrecision);
            int lastCubeZ = pieces[lastPiece].CubeZ << (CubeSizeLog2 - LogPrecision);

            int j2 = pieces[lastPiece].NumSegments * 4;
            for (int i = 0; i < 4; i++, j2++)
            {
                Coord3D coord = pieces[piece].Coords[i];
                pieces[lastPiece].Coords[j2] = new(
                    coord.X + cubeX - lastCubeX,
                    coord.Y + cubeY - lastCubeY,
                    coord.Z + cubeZ - lastCubeZ);
            }
        }
    }

    private TrackPiece[] ConvertAmigaTrack(
        int numPieces,
        byte[] piecePositions,
        byte[] angleAndTemplate,
        byte[] leftYId,
        byte[] rightYId,
        short[] leftYShift,
        short[] rightYShift)
    {
        ConvertedTemplates converted = s_converted.Value;
        TrackPiece[] pieces = new TrackPiece[numPieces];

        Array.Fill(_map, -1);

        int firstSegment = 0;
        for (int piece = 0; piece < numPieces; piece++)
        {
            // Piece's cube position within the world.
            int cubeX = piecePositions[piece] & 0x0f;
            int cubeZ = (piecePositions[piece] & 0xf0) >> 4;

            // Piece template information.
            byte c = angleAndTemplate[piece];
            int templateNum = c & 0x0f;
            int roughAngle = c & 0xc0;
            bool reverseOrder = (c & 0x10) != 0;

            PieceTemplate template = s_pieceTemplates[templateNum]
                ?? throw new InvalidDataException($"Track piece {piece} uses unknown template {templateNum}.");

            int numSegments = template.NumSegments;
            TrackPiece trackPiece = new(numSegments)
            {
                CubeX = cubeX,
                CubeY = 0,
                CubeZ = cubeZ,
                RoughPieceAngle = roughAngle * MaxAngle / 0x100,
                OppositeDirection = reverseOrder,
                CurveToLeft = template.CurveToLeft,
                Type = template.Type,
                LengthReduction = template.LengthReduction,
                SteeringAmount = template.SteeringAmount,
                FirstSegment = firstSegment,
                InitialColour = (rightYId[piece] & 0x80) >> 7,
                AngleAndTemplate = c,
                LeftYShift = leftYShift[piece],
                RightYShift = rightYShift[piece],
            };
            firstSegment += numSegments;

            _map[(cubeX * TrackCubes) + cubeZ] = piece;

            // Road/side surface colours: odd numbered pieces are light, even are dark.
            byte roadColour;
            if ((piece & 1) != 0)
            {
                roadColour = ScrBaseColour + 2;
                trackPiece.SidesColour = ScrBaseColour + 10;
            }
            else
            {
                roadColour = ScrBaseColour + 1;
                trackPiece.SidesColour = ScrBaseColour + 15;
            }

            int numCoords = numSegments + 1;

            StorePieceXZ(trackPiece, converted.XZ[templateNum]!, roughAngle, numCoords, reverseOrder);

            StorePieceY(
                trackPiece,
                converted.Y[leftYId[piece] & 0x7f],
                converted.Y[rightYId[piece] & 0x7f],
                leftYShift[piece],
                rightYShift[piece],
                numCoords);

            SetRoadColours(trackPiece, numSegments, roadColour);

            pieces[piece] = trackPiece;
        }

        JoinPieces(pieces, numPieces);

        return pieces;
    }

    private sealed record PieceTemplate(
        int NumSegments,
        bool CurveToLeft,
        int Type,
        int LengthReduction,
        int SteeringAmount,
        byte[] AmigaXZ);

    private sealed record ConvertedTemplates((int X, int Z)[]?[] XZ, int[][] Y);
}
