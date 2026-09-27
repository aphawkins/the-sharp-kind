// 'Bubble Bobble - The Sharp Kind' - Andy Hawkins 2026.
// 'rebb64' - github.com/zaidka/rebb64.
// Bubble Bobble (C) Taito 1986. C64 conversion by Software Creations 1987.

namespace BubbleBobbleSharpLib.Players;

internal sealed class PlayerTable
{
    internal const int Capacity = 2;

    private readonly byte[] _lives = new byte[Capacity];
    private readonly byte[] _reload = new byte[Capacity];
    private readonly byte[] _blowFacing = new byte[Capacity];
    private readonly byte[] _extendLetters = new byte[Capacity];
    private readonly byte[] _blowReload = [0x08, 0x08];
    private readonly byte[] _blowState = [0x88, 0x88];
    private readonly byte[] _blowVariant = [0x04, 0x04];
    private readonly byte[] _blowType = [0x04, 0x04];
    private readonly byte[] _driftRing = new byte[Capacity];
    private readonly byte[] _walkRing = new byte[Capacity];
    private readonly byte[] _blowRing = new byte[Capacity];

    internal Span<byte> Lives => _lives;

    internal Span<byte> Reload => _reload;

    internal Span<byte> BlowFacing => _blowFacing;

    internal Span<byte> ExtendLetters => _extendLetters;

    internal Span<byte> BlowReload => _blowReload;

    internal Span<byte> BlowState => _blowState;

    internal Span<byte> BlowVariant => _blowVariant;

    internal Span<byte> BlowType => _blowType;

    internal Span<byte> DriftRing => _driftRing;

    internal Span<byte> WalkRing => _walkRing;

    internal Span<byte> BlowRing => _blowRing;
}
