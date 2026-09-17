// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharpLib.Graphics;

namespace EliteSharpLib.Tests;

public class ExplosionScatterTests
{
    // The blast is a ball of debris, so every offset must sit inside the
    // radius. A square box passes the corner test below and fails this one.
    [Fact]
    public void ScatterOffsetStaysInsideTheRadius()
    {
        RenderRandom rng = new(new Random(0));

        for (int i = 0; i < 20000; i++)
        {
            Vector2 offset = EliteDraw.ScatterOffset(rng);

            Assert.True(offset.Length() <= 128, $"Offset {offset} is outside the radius.");
        }
    }

    // Written in the original's 256-wide space, so it must grow with the projection's focal length to keep the same size relative to the ship.
    [Fact]
    public void ScatterSpreadFollowsTheFocalLength()
    {
        float small = EliteDraw.ScatterSpread(64, 256);
        float large = EliteDraw.ScatterSpread(64, 512);

        Assert.Equal(2 * small, large, 3);
    }

    // At the original's own focal length the arithmetic is the original's:
    // a radius-128 offset times q/256, doubled.
    [Fact]
    public void ScatterSpreadMatchesTheOriginalAtFocus256()
        => Assert.Equal(2 * 64f / 256, EliteDraw.ScatterSpread(64, 256), 5);

    // ... and the disc must still be filled, not collapsed onto the middle or
    // the rim.
    [Fact]
    public void ScatterOffsetFillsTheDisc()
    {
        RenderRandom rng = new(new Random(0));
        int near = 0;
        int far = 0;

        for (int i = 0; i < 20000; i++)
        {
            float radius = EliteDraw.ScatterOffset(rng).Length();

            if (radius < 64)
            {
                near++;
            }
            else
            {
                far++;
            }
        }

        Assert.True(near > 0);
        Assert.True(far > near);
    }
}
