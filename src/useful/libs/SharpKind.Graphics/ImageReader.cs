// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics;

// Single entry point for loading image assets. The format is taken from the
// file's own magic bytes rather than its extension, so a mislabelled asset
// still loads.
//
// TGA is tried last because it is the only one of the three with nothing to
// recognise it by: the format has no signature at the start, and the version 2
// footer that would identify it is absent from the version 1 files this reads.
// What IsTga can offer is a header that could be one, so it is asked only once
// the formats that can answer for certain have said no.
public static class ImageReader
{
    public static FastBitmap Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        return bytes.Length == 0 ? new(0, 0)
            : PngReader.IsPng(bytes) ? PngReader.Decode(bytes)
            : BitmapReader.IsBmp(bytes) ? BitmapReader.Decode(bytes)
            : TgaReader.IsTga(bytes) ? TgaReader.Decode(bytes)
            : throw new SharpKindException($"Unrecognised image format: {path}");
    }
}
