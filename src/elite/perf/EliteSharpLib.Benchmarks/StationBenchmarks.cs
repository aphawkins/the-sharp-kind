// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using BenchmarkDotNet.Attributes;
using EliteSharp.Renditions.SixteenBit;
using EliteSharpLib.Graphics;
using EliteSharpLib.Missions;
using EliteSharpLib.Ships;
using Microsoft.Extensions.Logging.Abstractions;
using SharpKind.Assets;
using SharpKind.Fakes.Input;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;
using SharpKind.Input;
using SharpKind.Maths;

namespace EliteSharpLib.Benchmarks;

[JsonExporterAttribute.FullCompressed]
public class StationBenchmarks : IDisposable
{
    private const int ScreenWidth = 512;
    private const int ScreenHeight = 512;

    // Fixed seed: the station's shading jitter must not vary between runs.
    private static readonly Random s_random = new(12345);

    private readonly SoftwareGraphics _graphics;
    private readonly EliteDraw _draw;
    private readonly IShip _station;
    private readonly IShip _cobra;
    private readonly GameState _gameState;
    private bool _disposedValue;

    public StationBenchmarks()
    {
        FakeInput input = new();
        IAssetLocator assetLocator = BenchmarkAssets.Locator();
        SoftwareKeyboard keyboard = new(input);
        SharpKind.Abstraction.ScreenManager<Views.Screen, Views.IScreenController> views = new(keyboard);
        _gameState = new(views, new MissionRegistry([], NullLogger<MissionRegistry>.Instance));
        _graphics = SoftwareGraphics.Create(ScreenWidth, ScreenHeight, (_) => { }, assetLocator);
        _draw = new(
            _gameState,
            _graphics,
            new(ScreenWidth, ScreenHeight),
            assetLocator,
            new SixteenBitRendition(),
            new ZBufferRenderer(_graphics),
            new RenderRandom(s_random));

        ShipFactory factory = ShipFactory.Create(assetLocator, _draw, new RNG(s_random));
        _station = factory.CreateShip("Coriolis");
        _cobra = factory.CreateShip("CobraMk3");
        _station.Rotmat = VectorMaths.GetLeftHandedBasisMatrix;
        _cobra.Rotmat = VectorMaths.GetLeftHandedBasisMatrix;
    }

    // Camera-space Z, running from a distant speck to a station overflowing a 512x512 view - the shape of a docking approach.
    [Params(1000f, 250f)]
    public float Distance { get; set; }

    // Four settings differing in how often the quantiser is asked: never (Unlit/Nearest), per 4x4
    // cell (ordered dithering), or per pixel with a varying answer (Gouraud).
    [Params(
        GraphicsPreset.UnlitNearest,
        GraphicsPreset.LambertOrdered,
        GraphicsPreset.GouraudNearest,
        GraphicsPreset.GouraudOrdered)]
    public GraphicsPreset Preset { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _gameState.Config.Engine.Graphics.Shading = Preset switch
        {
            GraphicsPreset.UnlitNearest => ShadingModelKind.Unlit,
            GraphicsPreset.LambertOrdered => ShadingModelKind.Lambert,
            _ => ShadingModelKind.Gouraud,
        };

        _gameState.Config.Engine.Graphics.Quantisation =
            Preset is GraphicsPreset.LambertOrdered or GraphicsPreset.GouraudOrdered
                ? Quantisation.Ordered
                : Quantisation.Nearest;
    }

    [Benchmark(Baseline = true)]
    public void Station() => DrawFrame(_station);

    [Benchmark]
    public void Cobra() => DrawFrame(_cobra);

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _graphics.Dispose();
            }

            _disposedValue = true;
        }
    }

    // One tick's worth of universe drawing for a single object, as
    // Space.DrawUniverse does it.
    private void DrawFrame(IShip ship)
    {
        ship.Location = new Vector4(0, 0, Distance, 0);
        _draw.RenderStart();
        _draw.DrawObject(ship);
        _draw.RenderEnd();
    }
}
