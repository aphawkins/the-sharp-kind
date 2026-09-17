// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Graphics.Rendering;

// The New Kind capped its poly_chain at 100 and dropped anything after silently. That cap is
// reachable here, so this buffer grows instead; reused frame to frame, it settles at the busiest scene seen.
internal static class PolygonBuffer
{
    // Covers an ordinary scene without a resize; the old fixed cap was 100.
    internal const int InitialCapacity = 128;

    internal static void EnsureCapacity(ref PolygonData[] polys, int required)
    {
        if (required <= polys.Length)
        {
            return;
        }

        int capacity = polys.Length;

        while (capacity < required)
        {
            capacity *= 2;
        }

        Array.Resize(ref polys, capacity);
    }
}
