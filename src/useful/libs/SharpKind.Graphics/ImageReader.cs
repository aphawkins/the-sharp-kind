// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics;

// Single entry point for loading image assets. Format is taken from the file's magic bytes, not its extension, so a mislabelled asset still loads.
public static class ImageReader
{
    public static FastBitmap Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        return bytes.Length == 0 ? new(0, 0)
            : PngReader.IsPng(bytes) ? PngReader.Decode(bytes)
            : BitmapReader.IsBmp(bytes) ? BitmapReader.Decode(bytes)
            : throw new SharpKindException($"Unrecognised image format: {path}");
    }
}
