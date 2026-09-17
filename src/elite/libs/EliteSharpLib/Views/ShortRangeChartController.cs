// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Graphics;
using EliteSharpLib.Ships;
using EliteSharpLib.Types;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// The short range chart's behaviour: the cross-hair, the find-by-name
/// prompt, the distance readout and the row packing that decides which
/// planets get a name.
/// <para>
/// It works in screen space rather than galaxy space, which the other
/// controllers use - see <see cref="ShortRangeChartModel"/> for why - so it
/// takes the tier's layout metrics and serves either tier from them.
/// </para>
/// </summary>
internal sealed class ShortRangeChartController : IScreenController
{
    // The original packs names into 8-pixel text rows, and reserves the top
    // few for the header.
    private const int RowHeight = 8;
    private const int FirstPackedRow = 4;
    private const int PackedRows = 64;

    private readonly IEliteDraw _draw;
    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly List<ShortRangeChartLabel> _labels = [];
    private readonly List<ShortRangeChartPlanet> _planets = [];
    private readonly PlanetController _planet;
    private readonly PlayerShip _ship;
    private readonly IView<ShortRangeChartModel> _view;

    private Vector2 _cross;
    private int _crossTimer;
    private string _findName = string.Empty;
    private bool _isFind;

    internal ShortRangeChartController(
        GameState gameState,
        IEliteDraw draw,
        IKeyboard keyboard,
        PlanetController planet,
        PlayerShip ship,
        IView<ShortRangeChartModel> view)
    {
        _gameState = gameState;
        _draw = draw;
        _keyboard = keyboard;
        _planet = planet;
        _ship = ship;
        _view = view;
    }

    // Exposed for tests: the cross-hair's screen position.
    internal Vector2 Cross => _cross;

    // The bounds the cross-hair may be moved within, derived from the tier's
    // scale so one controller serves both: the 16-bit chart's clamps are the
    // 8-bit ones doubled.
    private (float MinX, float MaxX, float MinY, float MaxY) CrossBounds => (
        1,
        _draw.Layout.ViewportRight - 1,
        (18 * _draw.Layout.DesignScale) + 1,
        _draw.Layout.ViewportHeight - ((16 * _draw.Layout.DesignScale) + 1));

    public void Draw() => _view.Draw(BuildModel());

    public void HandleInput()
    {
        if (_isFind)
        {
            HandleFindInput();
            return;
        }

        if (_keyboard.IsPressed(ConsoleKey.O))
        {
            _cross = _draw.Layout.ViewportCentre;
            CalculateDistanceToPlanet();
        }

        if (_keyboard.IsPressed(ConsoleKey.D))
        {
            CalculateDistanceToPlanet();
        }

        if (_keyboard.IsPressed(ConsoleKey.S) || _keyboard.IsPressed(ConsoleKey.UpArrow))
        {
            MoveCross(0, -1);
        }

        if (_keyboard.IsPressed(ConsoleKey.X) || _keyboard.IsPressed(ConsoleKey.DownArrow))
        {
            MoveCross(0, 1);
        }

        if (_keyboard.IsPressed(ConsoleKey.OemComma) || _keyboard.IsPressed(ConsoleKey.LeftArrow))
        {
            MoveCross(-1, 0);
        }

        if (_keyboard.IsPressed(ConsoleKey.OemPeriod) || _keyboard.IsPressed(ConsoleKey.RightArrow))
        {
            MoveCross(1, 0);
        }

        if (_keyboard.IsPressed(ConsoleKey.F))
        {
            _isFind = true;
            _findName = string.Empty;
            _keyboard.ClearPressed();  // Clear the F so that it doesn't appear in the find word
        }
    }

    public void Reset()
    {
        _isFind = false;
        _findName = string.Empty;
        _labels.Clear();
        _planets.Clear();

        bool[] rowUsed = new bool[PackedRows];
        GalaxySeed glx = new(_gameState.Cmdr.Galaxy);
        float scale = _draw.Layout.DesignScale;
        Vector2 centre = _draw.Layout.ViewportCentre;

        for (int i = 0; i < 256; i++)
        {
            float dx = MathF.Abs(glx.D - _gameState.DockedPlanet.D);
            float dy = MathF.Abs(glx.B - _gameState.DockedPlanet.B);

            if ((dx >= 20) || (dy >= 38))
            {
                WaggleGalaxy(glx);
                continue;
            }

            // Convert to screen co-ords
            float px = ((glx.D - _gameState.DockedPlanet.D) * 4 * scale) + centre.X;
            float py = ((glx.B - _gameState.DockedPlanet.B) * 2 * scale) + centre.Y;

            int? row = PackedRow(py, scale, rowUsed);

            if (row is null)
            {
                WaggleGalaxy(glx);
                continue;
            }

            if (!rowUsed[row.Value])
            {
                rowUsed[row.Value] = true;
                _labels.Add(new(
                    new(px + (4 * scale), ((row.Value * RowHeight) - 5) * scale),
                    _planet.NamePlanet(glx).CapitaliseFirstLetter()));
            }

            // Circle size for a planet; carry_flag is left over from name generation, as the original did it.
            float blobSize = ((glx.F & 1) + 2 + _gameState.CarryFlag) * scale;
            _planets.Add(new(new(px, py), blobSize));

            WaggleGalaxy(glx);
        }

        _crossTimer = 0;
        CrossFromHyperspacePlanet();
        CalculateDistanceToPlanet();
    }

    public void Update()
    {
        if (_crossTimer > 0)
        {
            _crossTimer--;
            if (_crossTimer == 0)
            {
                CalculateDistanceToPlanet();
            }
        }
    }

    // Exposed for tests: the frame's data, including the caption/detail pair
    // the view prints.
    internal ShortRangeChartModel BuildModel()
    {
        (string caption, string detail) = CurrentLabel();

        return new(
            "SHORT RANGE CHART",
            _planets,
            _labels,
            _ship.Fuel,
            _cross,
            caption,
            detail);
    }

    /// <summary>
    /// Picks the label row a planet's plotted position falls in, packing it
    /// against a neighbour where that row is taken.
    /// </summary>
    /// <param name="py">The plotted y position, in screen pixels.</param>
    /// <param name="scale">The design scale the chart is drawn at.</param>
    /// <param name="rowUsed">Which rows already carry a label.</param>
    /// <returns>The row, or null where there is no row to label in.</returns>
    private static int? PackedRow(float py, float scale, bool[] rowUsed)
    {
        int row = (int)(py / (RowHeight * scale));

        // Checked before rowUsed is read: a design scale wider than the viewport puts py outside it, and the row with it.
        if (row is < FirstPackedRow or >= PackedRows)
        {
            return null;
        }

        // Only where there is a row to step into; the last one has none.
        if (rowUsed[row] && (row + 1 < PackedRows))
        {
            row++;
        }

        if (rowUsed[row])
        {
            row -= 2;
        }

        return row <= FirstPackedRow - 1 ? null : row;
    }

    // The find prompt while typing, otherwise the selected planet and how
    // far away it is.
    private (string Caption, string Detail) CurrentLabel()
    {
        if (_isFind)
        {
            return ("Planet Name?", _findName);
        }

        if (string.IsNullOrEmpty(_gameState.PlanetName))
        {
            return ("Unknown Planet", _findName);
        }

        return (
            _gameState.PlanetName,
            _gameState.DistanceToPlanet > 0
                ? $"Distance: {_gameState.DistanceToPlanet:N1} Light Years"
                : string.Empty);
    }

    // Typing a planet name into the find prompt.
    private void HandleFindInput()
    {
        if (_keyboard.IsPressed(ConsoleKey.Backspace) &&
            !string.IsNullOrEmpty(_findName))
        {
            _findName = _findName[..^1];
        }

        if (_keyboard.IsPressed(ConsoleKey.Enter))
        {
            _isFind = false;
            if (_planet.FindPlanetByName(_findName))
            {
                CrossFromHyperspacePlanet();
                CalculateDistanceToPlanet();
            }
            else
            {
                _gameState.PlanetName = string.Empty;
            }
        }

        (ConsoleKey key, ConsoleModifiers _) = _keyboard.LastPressed();
        if (key is >= ConsoleKey.A and <= ConsoleKey.Z)
        {
            _findName += (char)key;
        }
    }

    private void CalculateDistanceToPlanet()
    {
        float scale = _draw.Layout.DesignScale;
        Vector2 centre = _draw.Layout.ViewportCentre;
        Vector2 location = new()
        {
            X = ((_cross.X - centre.X) / (4 * scale)) + _gameState.DockedPlanet.D,
            Y = ((_cross.Y - centre.Y) / (2 * scale)) + _gameState.DockedPlanet.B,
        };

        _gameState.HyperspacePlanet = _planet.FindPlanet(_gameState.Cmdr.Galaxy, location);
        _gameState.PlanetName = _planet.NamePlanet(_gameState.HyperspacePlanet);
        _gameState.DistanceToPlanet = PlanetController.CalculateDistanceToPlanet(_gameState.DockedPlanet, _gameState.HyperspacePlanet);
        CrossFromHyperspacePlanet();
    }

    private void CrossFromHyperspacePlanet() => _cross = new(
        ((_gameState.HyperspacePlanet.D - _gameState.DockedPlanet.D) * 4 * _draw.Layout.DesignScale) + _draw.Layout.ViewportCentre.X,
        ((_gameState.HyperspacePlanet.B - _gameState.DockedPlanet.B) * 2 * _draw.Layout.DesignScale) + _draw.Layout.ViewportCentre.Y);

    /// <summary>
    /// Move the planet chart cross hairs to specified position.
    /// </summary>
    private void MoveCross(int dx, int dy)
    {
        _crossTimer = 5;
        (float minX, float maxX, float minY, float maxY) = CrossBounds;
        _cross = new(
            Math.Clamp(_cross.X + (dx * 4), minX, maxX),
            Math.Clamp(_cross.Y + (dy * 4), minY, maxY));
    }

    // The generator is stepped four times per planet whether or not the
    // planet is plotted, so the sequence stays in step with the original's.
    private void WaggleGalaxy(GalaxySeed glx)
    {
        _planet.WaggleGalaxy(glx);
        _planet.WaggleGalaxy(glx);
        _planet.WaggleGalaxy(glx);
        _planet.WaggleGalaxy(glx);
    }
}
