// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using EliteSharpLib.Ships;
using EliteSharpLib.Types;
using SharpKind.Input;

namespace EliteSharpLib.Views;

/// <summary>
/// The galactic chart's behaviour: the cross-hair, the find-by-name prompt
/// and the distance readout. Works entirely in galaxy space so the same
/// controller serves either tier's chart view.
/// </summary>
internal sealed class GalacticChartController : IScreenController
{
    // The original's 512-space clamps (x 1-510, y 37-293) expressed in galaxy units.
    private const float MinCrossX = 0.5f;
    private const float MaxCrossX = 255;
    private const float MinCrossY = 0;
    private const float MaxCrossY = 256;

    private readonly GameState _gameState;
    private readonly IKeyboard _keyboard;
    private readonly PlanetController _planet;
    private readonly PlayerShip _ship;
    private readonly List<GalacticChartStar> _stars = [];
    private readonly IView<GalacticChartModel> _view;

    private Vector2 _cross;
    private int _crossTimer;
    private string _findName = string.Empty;
    private bool _isFind;

    internal GalacticChartController(
        GameState gameState,
        IKeyboard keyboard,
        PlanetController planet,
        PlayerShip ship,
        IView<GalacticChartModel> view)
    {
        _gameState = gameState;
        _keyboard = keyboard;
        _planet = planet;
        _ship = ship;
        _view = view;
    }

    // Exposed for tests: the cross-hair's galaxy-space position.
    internal Vector2 Cross => _cross;

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
            _cross = new(_gameState.DockedPlanet.D, _gameState.DockedPlanet.B);
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
        GalaxySeed glx = new(_gameState.Cmdr.Galaxy);
        _stars.Clear();

        for (int i = 0; i < 256; i++)
        {
            _stars.Add(new(new(glx.D, glx.B), (glx.E | 0x50) < 0x90));

            _planet.WaggleGalaxy(glx);
            _planet.WaggleGalaxy(glx);
            _planet.WaggleGalaxy(glx);
            _planet.WaggleGalaxy(glx);
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

    // Exposed for tests: the caption/detail pair the view prints, and the rest of the frame's data.
    internal GalacticChartModel BuildModel()
    {
        (string caption, string detail) = CurrentLabel();

        return new(
            $"GALACTIC CHART {_gameState.Cmdr.GalaxyNumber + 1}",
            _stars,
            new(_gameState.DockedPlanet.D, _gameState.DockedPlanet.B),
            _ship.Fuel,
            _cross,
            caption,
            detail);
    }

    // The find prompt while typing, otherwise the selected planet and how far away it is.
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
                ? $"Distance: {_gameState.DistanceToPlanet:N1} Light Years "
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
        _gameState.HyperspacePlanet = _planet.FindPlanet(_gameState.Cmdr.Galaxy, _cross);
        _gameState.PlanetName = _planet.NamePlanet(_gameState.HyperspacePlanet);
        _gameState.DistanceToPlanet = PlanetController.CalculateDistanceToPlanet(_gameState.DockedPlanet, _gameState.HyperspacePlanet);
        CrossFromHyperspacePlanet();
    }

    private void CrossFromHyperspacePlanet()
        => _cross = new(_gameState.HyperspacePlanet.D, _gameState.HyperspacePlanet.B);

    /// <summary>
    /// Move the planet chart cross hairs to specified position.
    /// </summary>
    private void MoveCross(int dx, int dy)
    {
        _crossTimer = 5;
        _cross = new(
            Math.Clamp(_cross.X + dx, MinCrossX, MaxCrossX),
            Math.Clamp(_cross.Y + (dy * 2), MinCrossY, MaxCrossY));
    }
}
