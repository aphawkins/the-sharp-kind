// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

////using System.Diagnostics;
using System.Numerics;
using EliteSharp.Abstractions.Renditions;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Ships;
using SharpKind;
using SharpKind.Abstraction.Config;
using SharpKind.Assets;
using SharpKind.Assets.Models;
using SharpKind.Assets.Palettes;
using SharpKind.Graphics;
using SharpKind.Graphics.Rendering;
using SharpKind.Maths;

namespace EliteSharpLib.Graphics;

internal sealed class EliteDraw : IEliteDraw
{
    // Upper bound on points in any ship model, so the explosion buffer never has to grow.
    private const int MaxModelPoints = 100;

    // The original's projection was x * 256 / z against a 256-square view: a focal length of one screen height, i.e. a vertical FOV of 53.13 degrees.
    // Used directly rather than recomputed from 53 degrees, so the classic view stays bit-for-bit the classic view.
    private const float ClassicFocusFactor = 1.0f;

    private readonly FastColor _colorWhite;
    private readonly GameState _gameState;
    private readonly Vector4[] _pointList = new Vector4[MaxModelPoints];
    private readonly IPolygonRenderer _shipRenderer;
    private readonly RenderRandom _rng;
    private readonly bool _shadesShips;

    // Which stage is in use comes from the live config so Settings take effect next frame; the instances themselves never change.
    private readonly IShadingModel _unlit = new UnlitShading();
    private readonly IShadingModel _lambert = new LambertShading();
    private readonly IColourQuantiser _nearest;
    private readonly IColourQuantiser _dithered;

    internal EliteDraw(
        GameState gameState,
        IGraphics graphics,
        ScreenLayout screen,
        IAssetLocator assetLocator,
        IRendition rendition,
        IPolygonRenderer shipRenderer,
        RenderRandom rng)
    {
        ArgumentNullException.ThrowIfNull(rendition);
        ArgumentNullException.ThrowIfNull(screen);

        _gameState = gameState;
        Graphics = graphics;
        _shipRenderer = shipRenderer;
        _rng = rng;

        Layout = new(
            screen.ScreenWidth,
            screen.ScreenHeight,
            rendition.ConsoleHeight,
            rendition.DesignScale);
        Palette = PaletteReader.Read(assetLocator.PalettePath);
        _shadesShips = rendition.ShadesShips;

        // Shading invents colours the assets never carried, so it is bound by the same palette limits as the asset validator.
        _nearest = assetLocator.Colours.PaletteNamesEveryColour
            ? new PaletteQuantiser(Palette.Values)
            : new ChannelGridQuantiser(assetLocator.Colours.ChannelBits);
        _dithered = new OrderedDitherQuantiser(_nearest);

        // Only worth blending where the rendition has shades to blend through: an indexed palette quantises a gradient back to a flat fill's few steps anyway.
        BlendsShades = !assetLocator.Colours.PaletteNamesEveryColour;

        // After Palette: the rendition looks its colours up through this.
        Ships = rendition.CreateShipColours(this);
        _colorWhite = Palette["White"];
    }

    public ViewLayout Layout { get; }

    // Derived from the tier's height, holding vertical FOV constant; not tied to DesignScale, which only affects chrome.
    // Read from the live config, not captured, so the Field of View setting shows next frame.
    public float Focus => Layout.ScreenHeight * FocusFactor(_gameState.Config.Engine.FieldOfView);

    public IRandomSource Jitter => _rng;

    public IGraphics Graphics { get; }

    public IPaletteCollection Palette { get; }

    public ShipColours Ships { get; }

    // Gouraud is the only model whose colour varies within a face, and only where the rendition can show the difference.
    public bool ShadesPerVertex
        => BlendsShades
            && Shading != _unlit
            && _gameState.Config.Engine.Graphics.Shading == ShadingModelKind.Gouraud;

    // Whether the rendition has enough colours for a blend to survive quantising. Set once: it is the rendition's, not the commander's.
    private bool BlendsShades { get; }

    private IShadingModel Shading
    {
        get
        {
            GraphicsConfigSettings graphics = _gameState.Config.Engine.Graphics;

            return _shadesShips
                && graphics.FillMode == FillMode.Solid
                && graphics.Shading is ShadingModelKind.Lambert or ShadingModelKind.Gouraud
                    ? _lambert
                    : _unlit;
        }
    }

    // Dithering an unshaded world would only dither colours that are already displayable, so it follows the shading model.
    private IColourQuantiser Quantiser
        => _gameState.Config.Engine.Graphics.Quantisation == Quantisation.Ordered && Shading != _unlit
            ? _dithered
            : _nearest;

    // depths is per-point camera-space depth (for the z-buffer strategy); z is one whole-face key (for the painter strategy).
    // Decal faces share their base face's z key, plus a small near bias in depths, to win the per-pixel test.
    public void DrawPolygonFilled(Vector2[] points, float[] depths, FastColor faceColor, float z)
    {
        // A dither travels with the polygon: only the fill can apply it, per pixel; ShadeFace already resolved anything else.
        IColourQuantiser quantiser = Quantiser;

        _shipRenderer.Submit(points, depths, faceColor, z, quantiser.IsPositionDependent ? quantiser : null);
    }

    // The quantiser always travels with the polygon here: a blended colour differs per pixel, so nothing could be resolved for the whole face in ShadeVertex.
    public void DrawPolygonFilled(Vector2[] points, float[] depths, FastColor[] cornerColors, float z)
        => _shipRenderer.Submit(points, depths, cornerColors, z, Quantiser);

    // Read from the live config so a Settings toggle shows next frame, as ConfigPolygonRenderer does. Wireframe has no faces to light.
    public FastColor ShadeFace(FastColor faceColour, Vector3 cameraNormal, byte fullyLit)
    {
        FastColor shaded = Shading.Shade(faceColour, cameraNormal, fullyLit);

        // A dither must be asked per pixel, so it is left to the fill unquantised; anything else resolves here, once.
        IColourQuantiser quantiser = Quantiser;

        return quantiser.IsPositionDependent ? shaded : quantiser.Quantise(shaded, 0, 0);
    }

    // Deliberately unquantised, unlike ShadeFace: the fill blends between corners and quantises per pixel.
    // Quantising here first would blend between already-reduced values, a worse approximation of the same curve.
    public FastColor ShadeVertex(FastColor faceColour, Vector3 cameraNormal, byte fullyLit)
        => Shading.Shade(faceColour, cameraNormal, fullyLit);

    public void SetFullScreenClipRegion() => Graphics.SetClipRegion(new(0, 0), Layout.ScreenWidth, Layout.ScreenHeight);

    /// <summary>
    /// Draws an object in the universe. (Ship, Planet, Sun etc).
    /// </summary>
    public void DrawObject(IObject obj)
    {
        if (obj.Flags.HasFlag(ShipProperties.Explosion))
        {
            DrawExplosion((IShip)obj);
            return;
        }

        if (obj.Location.Z <= 0)
        {
            return;
        }

        if (obj.Id == ObjectIds.Planet)
        {
            obj.Draw();
            return;
        }

        if (obj.Id == ObjectIds.Sun)
        {
            obj.Draw();
            return;
        }

        if (MathF.Abs(obj.Location.X) > obj.Location.Z ||
            MathF.Abs(obj.Location.Y) > obj.Location.Z)
        {
            return;
        }

        obj.Draw();
    }

    public void RenderEnd() => _shipRenderer.EndFrame();

    public void RenderStart() => _shipRenderer.StartFrame();

    // One debris offset, uniform inside a disc of radius 128. Points outside are re-rolled rather than clamped, which would pile rejected corners onto the rim.
    internal static Vector2 ScatterOffset(IRandomSource rng)
    {
        while (true)
        {
            Vector2 offset = new(rng.Random(-128, 128), rng.Random(-128, 128));

            if ((offset.X * offset.X) + (offset.Y * offset.Y) <= 128 * 128)
            {
                return offset;
            }
        }
    }

    // Offset is a radius-128 disc and q a spread, both in the original's 256-wide space, so the result follows the focal length rather than being pixels.
    // Without the focal term the cloud would read twice as large on a 320-wide screen as on a 640-wide one.
    internal static float ScatterSpread(float q, float focus) => 2 * q / 256 * (focus / 256);

    // Half the screen height over the tangent of the half-angle: the focal length for a vertical FOV of that many degrees, expressed as a factor of the height.
    private static float FocusFactor(int? fieldOfView)
        => fieldOfView is null
            ? ClassicFocusFactor
            : 0.5f / MathF.Tan(float.DegreesToRadians(fieldOfView.Value) / 2);

    // The age is not advanced here: Space.AgeExplosion sets it, so the cloud runs at the game's rate rather than the frame rate.
    private void DrawExplosion(IShip ship)
    {
        // Flagged for removal here; Space removes it from the universe on the next tick.
        if (ship.Flags.HasFlag(ShipProperties.Remove))
        {
            return;
        }

        if (ship.Location.Z <= 0)
        {
            return;
        }

        // Needs the rotation matrix's basis vectors transposed relative to the point-transform below (see ShipBase.Draw).
        Matrix4x4 cameraMat = ship.Rotmat;
        (cameraMat.M12, cameraMat.M21) = (cameraMat.M21, cameraMat.M12);
        (cameraMat.M13, cameraMat.M31) = (cameraMat.M31, cameraMat.M13);
        (cameraMat.M23, cameraMat.M32) = (cameraMat.M32, cameraMat.M23);

        Vector4 camera_vec = Vector4.Transform(ship.Location, cameraMat);
        camera_vec = VectorMaths.UnitVector(camera_vec);

        foreach (FaceNormal faceNormal in ship.Model.FaceNormals)
        {
            Vector4 vec = VectorMaths.UnitVector(faceNormal.Direction);
            float cos_angle = VectorMaths.VectorDotProduct(vec, camera_vec);
            faceNormal.Visible = cos_angle < -0.13;
        }

        int np = ProjectExplosionPoints(ship);

        float z = ship.Location.Z;
        float q = z >= 0x2000 ? 254 : (int)(z / 32) | 1;
        float pr = ship.ExpDelta * 256 / q;

        ////  if (pr > 0x1C00)
        ////      q = 254;
        ////  else
        q = pr / 32;

        DrawExplosionParticles(np, q, ParticleColour(ship.ExpDelta));
    }

    // The original cloud was solid white for its whole life (the BBC couldn't draw anything else); fading it reads as debris thinning rather than a block vanishing.
    // Gated like shading: an indexed rendition would quantise the blend straight back to white.
    private FastColor ParticleColour(float expDelta)
        => BlendsShades
            ? new((byte)(255 - (expDelta * 200 / 256)), _colorWhite.R, _colorWhite.G, _colorWhite.B)
            : _colorWhite;

    private int ProjectExplosionPoints(IShip ship)
    {
        int np = 0;

        // Through the interface: Projector is a default implementation, not in scope on the implementing class.
        PerspectiveProjector projector = ((IEliteDraw)this).Projector;

        for (int i = 0; i < ship.Model.Points.Count; i++)
        {
            if (ship.Model.Points[i].FaceNormals.Any(x => x.Visible))
            {
                Vector4 vec = Vector4.Transform(ship.Model.Points[i].Coords, ship.Rotmat);
                Vector4 r = vec + ship.Location;
                Vector2 position = projector.Project(r.X, r.Y, r.Z);
                _pointList[np].X = position.X;
                _pointList[np].Y = position.Y;
                np++;
            }
        }

        return np;
    }

    private void DrawExplosionParticles(int np, float q, in FastColor color)
    {
        float spread = ScatterSpread(q, Focus);

        // Blocks are chrome (the original's pixel), not geometry, so they follow DesignScale like the rest of the render.
        int blockScale = (int)Layout.DesignScale;

        for (int cnt = 0; cnt < np; cnt++)
        {
            Vector2 point = new(_pointList[cnt].X, _pointList[cnt].Y);

            for (int i = 0; i < 16; i++)
            {
                Vector2 position = (ScatterOffset(_rng) * spread) + point;

                int sizex = _rng.Random(1, 3) * blockScale;
                int sizey = _rng.Random(1, 3) * blockScale;

                DrawExplosionBlock(position, sizex, sizey, color);
            }
        }
    }

    private void DrawExplosionBlock(Vector2 position, int sizex, int sizey, in FastColor color)
    {
        for (int psy = 0; psy < sizey; psy++)
        {
            for (int psx = 0; psx < sizex; psx++)
            {
                Graphics.DrawPixel(new(position.X + psx, position.Y + psy), color);
            }
        }
    }
}
