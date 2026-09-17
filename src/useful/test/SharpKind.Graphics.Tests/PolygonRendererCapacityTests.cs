// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Numerics;
using Moq;
using SharpKind.Graphics.Rendering;

namespace SharpKind.Graphics.Tests;

// Regression: renderers used to hold a fixed 100-polygon chain and drop everything after it silently; a busy scene passes 100 easily.
public class PolygonRendererCapacityTests
{
    private const int PolyCount = 500;

    [Fact]
    public void PainterRendererDrawsEveryPolygonPastTheOldCap()
    {
        Mock<IGraphics> graphics = MockSetup.MockGraphics();
        PainterRenderer renderer = new(graphics.Object);

        SubmitFrame(renderer);

        graphics.Verify(
            x => x.DrawPolygonFilled(It.IsAny<Vector2[]>(), It.IsAny<FastColor>(), It.IsAny<IColourQuantiser?>()),
            Times.Exactly(PolyCount));
    }

    [Fact]
    public void ZBufferRendererDrawsEveryPolygonPastTheOldCap()
    {
        Mock<IGraphics> graphics = MockSetup.MockGraphics();
        ZBufferRenderer renderer = new(graphics.Object);

        SubmitFrame(renderer);

        graphics.Verify(
            x => x.DrawPolygonFilledDepth(It.IsAny<Vector2[]>(), It.IsAny<float[]>(), It.IsAny<FastColor>(), It.IsAny<IColourQuantiser?>()),
            Times.Exactly(PolyCount));
    }

    // A second frame reuses the grown buffer; it must still draw the lot,
    // and only the lot - StartFrame resets the count, it doesn't shrink.
    [Fact]
    public void ZBufferRendererCountIsPerFrameAfterGrowing()
    {
        Mock<IGraphics> graphics = MockSetup.MockGraphics();
        ZBufferRenderer renderer = new(graphics.Object);

        SubmitFrame(renderer);
        graphics.Invocations.Clear();
        SubmitFrame(renderer);

        graphics.Verify(
            x => x.DrawPolygonFilledDepth(It.IsAny<Vector2[]>(), It.IsAny<float[]>(), It.IsAny<FastColor>(), It.IsAny<IColourQuantiser?>()),
            Times.Exactly(PolyCount));
    }

    private static void SubmitFrame(IPolygonRenderer renderer)
    {
        renderer.StartFrame();

        for (int i = 0; i < PolyCount; i++)
        {
            Vector2[] points = [new(i, 0), new(i + 1, 0), new(i, 1)];
            float[] depths = [i + 1, i + 1, i + 1];
            renderer.Submit(points, depths, new FastColor(255, 255, 255, 255), i + 1);
        }

        renderer.EndFrame();
    }
}
