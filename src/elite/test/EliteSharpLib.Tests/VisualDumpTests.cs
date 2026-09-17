// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using EliteSharpLib.Tests.Missions;
using EliteSharpLib.Views;
using SharpKind.Abstraction;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;

namespace EliteSharpLib.Tests;

// Renders ships and saves BMP files for visual inspection (under EliteFrames in the temp dir).
// Overlap/interpenetration scenes exercise the hidden-surface handling the painter's algorithm gets wrong per face.
[Trait("Level", "Integration")]
public class VisualDumpTests
{
    [Fact]
    public void DumpShipFrames()
    {
        string outDir = Path.Combine(Path.GetTempPath(), "EliteFrames");
        Directory.CreateDirectory(outDir);

        FastBitmap? lastFrame = null;
        using SoftwareGraphics graphics = SoftwareGraphics.Create(512, 512, b => lastFrame = b, TestAssets.Locator());
        GameState gameState = new(new ScreenManager<Screen, IScreenController>(new FakeKeyboard()), TestMissions.Registry());
        ZBufferRenderer shipRenderer = new(graphics);
        RenderRandom rng = new(new Random(0));
        EliteDraw draw = new(gameState, graphics, new(512, 512), TestAssets.Locator(), new SixteenBitRendition(), shipRenderer, rng);
        ShipFactory factory = ShipFactory.Create(TestAssets.Locator(), draw, new RNG(new Random(0)));

        void RenderAndSave(string name, params IShip[] ships)
        {
            graphics.Clear();
            draw.RenderStart();
            foreach (IShip ship in ships)
            {
                ship.Draw();
            }

            draw.RenderEnd();
            graphics.ScreenUpdate();
            Assert.NotNull(lastFrame);
            BitmapWriter.Write(lastFrame, Path.Combine(outDir, name));
        }

        // a lone ship at several orientations (self-occlusion of its own
        // rear faces is where "bits of hidden surfaces show through")
        IShip cobra = factory.CreateShip("CobraMk3");
        cobra.Location = new(0, 0, 420, 0);
        for (int i = 0; i < 4; i++)
        {
            float angle = 0.4f + (i * 0.8f);
            cobra.Rotmat = Matrix4x4.CreateRotationY(angle) * Matrix4x4.CreateRotationX(0.3f + (i * 0.2f));
            RenderAndSave($"frame_cobra_{i}.bmp", cobra);
        }

        // two ships overlapping on screen with interleaved depth ranges
        IShip viper = factory.CreateShip("Viper");
        cobra.Location = new(-15, 0, 420, 0);
        cobra.Rotmat = Matrix4x4.CreateRotationY(0.9f) * Matrix4x4.CreateRotationX(0.4f);
        viper.Location = new(25, 10, 460, 0);
        viper.Rotmat = Matrix4x4.CreateRotationY(-0.7f) * Matrix4x4.CreateRotationX(-0.2f);
        RenderAndSave("frame_overlap.bmp", cobra, viper);

        // ships whose models carry 2-point line faces (hull window detail)
        // over large flat hull faces
        IShip transporter = factory.CreateShip("Transporter");
        transporter.Location = new(0, 0, 110, 0);
        for (int i = 0; i < 4; i++)
        {
            transporter.Rotmat =
                Matrix4x4.CreateRotationY(-0.6f + (i * 0.7f)) * Matrix4x4.CreateRotationX(-0.35f + (i * 0.25f));
            RenderAndSave($"frame_transporter_{i}.bmp", transporter);
        }

        // a full spin, where wrap-around hull faces meet far-side decals
        for (int i = 0; i < 12; i++)
        {
            transporter.Rotmat =
                Matrix4x4.CreateRotationY(i * MathF.PI / 6) * Matrix4x4.CreateRotationX(0.25f);
            RenderAndSave($"frame_transporter_spin_{i:00}.bmp", transporter);
        }

        // the Cobra seen from behind (the red engine plates)
        IShip cobraRear = factory.CreateShip("CobraMk3");
        cobraRear.Location = new(0, 0, 260, 0);
        cobraRear.Rotmat = Matrix4x4.CreateRotationY(MathF.PI - 0.25f) * Matrix4x4.CreateRotationX(0.15f);
        RenderAndSave("frame_cobra_rear.bmp", cobraRear);

        // two hulls actually intersecting - unsortable for a per-face
        // painter's algorithm, only per-pixel depth gets this right
        IShip krait = factory.CreateShip("Krait");
        cobra.Location = new(0, 0, 430, 0);
        cobra.Rotmat = Matrix4x4.CreateRotationY(0.5f);
        krait.Location = new(10, 5, 430, 0);
        krait.Rotmat = Matrix4x4.CreateRotationY(-1.1f) * Matrix4x4.CreateRotationX(0.6f);
        RenderAndSave("frame_interpenetrate.bmp", cobra, krait);
    }

    // The sharpest test of hidden-line removal: the missile's four double-sided fin plates always
    // survive a backface cull, so only occlusion can hide the pair behind the body.
    [Fact]
    public void DumpWireframeMissileFrames()
    {
        string outDir = Path.Combine(Path.GetTempPath(), "EliteFrames");
        Directory.CreateDirectory(outDir);

        FastBitmap? lastFrame = null;
        using SoftwareGraphics graphics = SoftwareGraphics.Create(512, 512, b => lastFrame = b, TestAssets.Locator());
        WireframeRenderer shipRenderer = new(graphics, TestAssets.Locator());
        GameState gameState = new(new ScreenManager<Screen, IScreenController>(new FakeKeyboard()), TestMissions.Registry());
        RenderRandom rng = new(new Random(0));
        EliteDraw draw = new(gameState, graphics, new(512, 512), TestAssets.Locator(), new SixteenBitRendition(), shipRenderer, rng);
        ShipFactory factory = ShipFactory.Create(TestAssets.Locator(), draw, new RNG(new Random(0)));

        IShip missile = factory.CreateShip("Missile");
        missile.Location = new(0, 0, 120, 0);

        for (int i = 0; i < 6; i++)
        {
            missile.Rotmat =
                Matrix4x4.CreateRotationY(i * MathF.PI / 6) * Matrix4x4.CreateRotationX(0.35f);

            graphics.Clear();
            draw.RenderStart();
            missile.Draw();
            draw.RenderEnd();
            graphics.ScreenUpdate();

            Assert.NotNull(lastFrame);
            BitmapWriter.Write(lastFrame, Path.Combine(outDir, $"frame_wire_missile_{i}.bmp"));
        }

        // the same poses filled, as the reference for what should be visible
        ZBufferRenderer solidRenderer = new(graphics);
        EliteDraw solidDraw = new(
            gameState,
            graphics,
            new(512, 512),
            TestAssets.Locator(),
            new SixteenBitRendition(),
            solidRenderer,
            rng);
        IShip solidMissile = ShipFactory.Create(TestAssets.Locator(), solidDraw, new RNG(new Random(0))).CreateShip("Missile");
        solidMissile.Location = new(0, 0, 120, 0);

        for (int i = 0; i < 6; i++)
        {
            solidMissile.Rotmat =
                Matrix4x4.CreateRotationY(i * MathF.PI / 6) * Matrix4x4.CreateRotationX(0.35f);

            graphics.Clear();
            solidDraw.RenderStart();
            solidMissile.Draw();
            solidDraw.RenderEnd();
            graphics.ScreenUpdate();

            Assert.NotNull(lastFrame);
            BitmapWriter.Write(lastFrame, Path.Combine(outDir, $"frame_solid_missile_{i}.bmp"));
        }
    }

    // Both strategies must produce the same silhouette, but not pixel-identical inside it: the
    // z-buffer's depth-owned seam differs from the painter's later-face-wins, a thin edge effect.
    [Fact]
    public void PainterAndZBufferAgreeOnSilhouetteForNonDecalGeometry()
    {
        (uint[] Pixels, int Width, int Height) RenderAsteroid(Func<IGraphics, IPolygonRenderer> createRenderer)
        {
            FastBitmap? lastFrame = null;
            using SoftwareGraphics graphics = SoftwareGraphics.Create(512, 512, b => lastFrame = b, TestAssets.Locator());
            IPolygonRenderer shipRenderer = createRenderer(graphics);
            GameState gameState = new(new ScreenManager<Screen, IScreenController>(new FakeKeyboard()), TestMissions.Registry());
            RenderRandom rng = new(new Random(0));
            EliteDraw draw = new(gameState, graphics, new(512, 512), TestAssets.Locator(), new SixteenBitRendition(), shipRenderer, rng);
            ShipFactory factory = ShipFactory.Create(TestAssets.Locator(), draw, new RNG(new Random(0)));

            IShip asteroid = factory.CreateShip("Asteroid");
            asteroid.Location = new(0, 0, 300, 0);
            asteroid.Rotmat = Matrix4x4.CreateRotationY(0.9f) * Matrix4x4.CreateRotationX(0.4f);

            graphics.Clear();
            draw.RenderStart();
            asteroid.Draw();
            draw.RenderEnd();
            graphics.ScreenUpdate();

            Assert.NotNull(lastFrame);
            int width = lastFrame.Width;
            int height = lastFrame.Height;
            uint[] pixels = new uint[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    pixels[(y * width) + x] = lastFrame.GetPixel(x, y).Argb;
                }
            }

            return (pixels, width, height);
        }

        (uint[] painterPixels, int width, int height) = RenderAsteroid(g => new PainterRenderer(g));
        (uint[] depthBufferPixels, _, _) = RenderAsteroid(g => new ZBufferRenderer(g));

        Assert.Equal(width * height, painterPixels.Length);

        const uint background = 0xFF000000;
        int lit = 0;
        int differing = 0;
        for (int i = 0; i < painterPixels.Length; i++)
        {
            Assert.Equal(painterPixels[i] == background, depthBufferPixels[i] == background);

            if (painterPixels[i] != background)
            {
                lit++;
                if (painterPixels[i] != depthBufferPixels[i])
                {
                    differing++;
                }
            }
        }

        Assert.True(lit > 10_000, $"the asteroid should cover a substantial area, covered {lit}");
        Assert.True(differing < lit / 50, $"{differing} of {lit} lit pixels differ, more than face seams explain");
    }
}
