// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind;

public readonly struct FastColor : IEquatable<FastColor>
{
    public FastColor(uint argbColor)
    {
        Argb = argbColor;

        A = (byte)(argbColor >> 24);
        R = (byte)(argbColor >> 16);
        G = (byte)(argbColor >> 8);
        B = (byte)argbColor;
    }

    public FastColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;

        Argb = (uint)((a << 24) | (r << 16) | (g << 8) | b);
    }

    public byte A { get; }

    public uint Argb { get; }

    public byte B { get; }

    public byte G { get; }

    public byte R { get; }

    public static bool operator !=(in FastColor left, in FastColor right) => !(left == right);

    public static bool operator ==(in FastColor left, in FastColor right) => left.Argb == right.Argb;

    public static FastColor FromUInt32(uint argbColor) => new(argbColor);

    // Source-over composite onto an opaque destination; result is always opaque since a framebuffer is.
    public static FastColor Blend(in FastColor source, in FastColor destination)
        => source.A switch
        {
            0 => destination,
            255 => source,
            _ => new(
                255,
                BlendChannel(source.R, destination.R, source.A),
                BlendChannel(source.G, destination.G, source.A),
                BlendChannel(source.B, destination.B, source.A)),
        };

    public static uint ToUInt32(in FastColor color) => color.Argb;

    public override bool Equals(object? obj) => obj is FastColor other && Equals(other);

    public bool Equals(FastColor other) => this == other;

    public override int GetHashCode() => Argb.GetHashCode();

    public override string ToString() => $"0x{Argb:X}";

    private static byte BlendChannel(byte source, byte destination, byte alpha)
    {
        // +128 and >> 8 round the divide by 255 to nearest without a division.
        int scaled = (source * alpha) + (destination * (255 - alpha));
        return (byte)((scaled + 128 + ((scaled + 128) >> 8)) >> 8);
    }
}
