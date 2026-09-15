// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Assets;
using SharpKind;

namespace EliteSharp.Abstractions.Views;

/// <summary>
/// The HUD: the canopy, the dials, the compass and the scanner's lollipops.
/// Every position here is an offset into the console band at the foot of the
/// HUD art, and the two tiers' bands are different sizes (8-bit 320x56
/// against 16-bit 640x128), so the layout is entirely the subclass's - this
/// base holds only the drawing that every tier does the same way.
/// <para>
/// It reads no game state. What is on the scanner, how full the dials are and
/// where the compass points all arrive on <see cref="ScannerModel"/>, the same
/// as any other screen, which is what lets the HUD live in a rendition
/// rather than in the game.
/// </para>
/// </summary>
public abstract class ScannerViewBase : IView<ScannerModel>
{
    protected ScannerViewBase(IViewSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        Surface = surface;
    }

    /// <summary>
    /// Gets what this draws on.
    /// </summary>
    protected IViewSurface Surface { get; }

    /// <summary>
    /// Gets the scanner's centre in screen coordinates, which the lollipops
    /// plot around.
    /// </summary>
    protected abstract Vector2 ScannerCentre { get; }

    /// <summary>
    /// Gets the half-height and half-width of the lollipop area, in the
    /// scanner's own units. Anything outside it is off the scanner.
    /// </summary>
    protected abstract (float Y, float X) ScannerExtent { get; }

    /// <summary>
    /// Gets the dial bar's height in pixels, including its top and bottom rules.
    /// </summary>
    protected abstract int DialBarHeight { get; }

    /// <summary>
    /// Gets the length in pixels of a full dial bar, which is what a dial's
    /// 0-to-1 reading is scaled by.
    /// </summary>
    protected abstract float DialBarWidth { get; }

    /// <summary>
    /// Gets how far in pixels the roll and pitch indicators travel either side
    /// of their dial's centre.
    /// </summary>
    protected abstract float IndicatorTravel { get; }

    /// <summary>
    /// Gets the thickness in pixels of the roll and pitch indicators.
    /// </summary>
    protected abstract int IndicatorWidth { get; }

    protected abstract Vector2 ShieldFrontPosition { get; }

    protected abstract Vector2 ShieldRearPosition { get; }

    protected abstract Vector2 FuelPosition { get; }

    protected abstract Vector2 CabinTempPosition { get; }

    protected abstract Vector2 LaserTempPosition { get; }

    protected abstract Vector2 AltitudePosition { get; }

    /// <summary>
    /// Gets the topmost energy bank's position; the other three follow below it.
    /// </summary>
    protected abstract Vector2 EnergyPosition { get; }

    protected abstract float EnergyBankSpacing { get; }

    protected abstract Vector2 SpeedPosition { get; }

    protected abstract float SpeedHeight { get; }

    protected abstract Vector2 RollPosition { get; }

    protected abstract Vector2 PitchPosition { get; }

    protected abstract Vector2 CompassCentre { get; }

    protected abstract float CompassRadius { get; }

    protected abstract float CompassDotRadius { get; }

    protected abstract Vector2 MissilePosition { get; }

    protected abstract float MissileSpacing { get; }

    protected abstract Vector2 StationIndicatorPosition { get; }

    protected abstract Vector2 EcmIndicatorPosition { get; }

    /// <summary>
    /// Gets the dial's top rule colour, which the roll, pitch and speed
    /// indicators also use.
    /// </summary>
    protected abstract FastColor DialTopColor { get; }

    protected abstract FastColor DialBodyColor { get; }

    protected abstract FastColor DialBottomColor { get; }

    protected abstract FastColor SpeedWarningColor { get; }

    /// <summary>
    /// Gets what this tier paints each sort of ship. The lollipops take their
    /// colours from here, and so does the beam a ship fires, so the two cannot
    /// drift apart.
    /// </summary>
    protected abstract ShipColours Ships { get; }

    /// <summary>
    /// Draws the HUD art itself, under the readings. It is a full-screen
    /// overlay rather than a strip along the bottom: the frame around the
    /// viewport is the cockpit window the universe is seen through, so it is
    /// part of the same artwork as the console below it, and everything it
    /// does not ink is transparent.
    /// </summary>
    public void DrawHud() => Surface.Graphics.DrawImage(nameof(ImageType.Hud), new(0, 0));

    public void Draw(ScannerModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        DrawHud();
        DisplaySpeed(model);
        DisplayIndicator(model.Pitch, ScannerRelative(PitchPosition));
        DisplayIndicator(model.Roll, ScannerRelative(RollPosition));
        DisplayDial(model.ShieldFront, ShieldFrontPosition);
        DisplayDial(model.ShieldRear, ShieldRearPosition);
        DisplayDial(model.Altitude, AltitudePosition);
        DisplayEnergy(model);
        DisplayDial(model.CabinTemperature, CabinTempPosition);
        DisplayDial(model.LaserTemperature, LaserTempPosition);
        DisplayDial(model.Fuel, FuelPosition);
        DisplayMissiles(model);

        if (model.IsDocked)
        {
            return;
        }

        UpdateScanner(model);
        UpdateCompass(model);

        if (model.IsStationPresent)
        {
            Surface.Graphics.DrawImage(nameof(ImageType.Station), ScannerRelative(StationIndicatorPosition));
        }

        if (model.IsEcmActive)
        {
            Surface.Graphics.DrawImage(nameof(ImageType.ECM), ScannerRelative(EcmIndicatorPosition));
        }
    }

    /// <summary>
    /// Turns a console-relative position into a screen one.
    /// </summary>
    /// <param name="position">The position, relative to the console band's top left.</param>
    /// <returns>The same position in screen coordinates.</returns>
    protected Vector2 ScannerRelative(Vector2 position)
        => new(Surface.Layout.ViewportLeft + position.X, Surface.Layout.ViewportHeight + position.Y);

    private static string MissileImage(MissileIndicator missile) => missile switch
    {
        MissileIndicator.Armed => nameof(ImageType.MissileYellow),
        MissileIndicator.Locked => nameof(ImageType.MissileRed),
        _ => nameof(ImageType.MissileGreen),
    };

    /// <summary>
    /// Draws one dial, or nothing when it reads empty - the controller has
    /// already applied whatever minimum that dial is worth showing.
    /// </summary>
    private void DisplayDial(float reading, Vector2 position)
    {
        if (reading > 0)
        {
            DisplayDialBar(reading * DialBarWidth, position);
        }
    }

    /// <summary>
    /// Draw an indicator bar. Used for shields and energy banks.
    /// </summary>
    private void DisplayDialBar(float len, Vector2 position)
    {
        Vector2 origin = ScannerRelative(position);
        float x = origin.X;
        float y = origin.Y;
        int last = DialBarHeight - 1;

        Surface.Graphics.DrawLine(new(x, y), new(x + len - 1, y), DialTopColor);
        int i = 1;
        Surface.Graphics.DrawLine(new(x, y + i), new(x + len - 1, y + i), DialTopColor);

        for (i = 2; i < last; i++)
        {
            Surface.Graphics.DrawLine(new(x, y + i), new(x + len - 1, y + i), DialBodyColor);
        }

        Surface.Graphics.DrawLine(new(x, y + i), new(x + len - 1, y + i), DialBottomColor);
    }

    /// <summary>
    /// Display the energy banks, topmost first. Which bank holds what is the
    /// controller's arithmetic; this only stacks them.
    /// </summary>
    private void DisplayEnergy(ScannerModel model)
    {
        Vector2 bank = EnergyPosition;

        for (int i = 0; i < model.EnergyBanks.Count; i++)
        {
            if (model.EnergyBanks[i] > 0)
            {
                DisplayDialBar(model.EnergyBanks[i] * DialBarWidth, bank with { Y = bank.Y + (i * EnergyBankSpacing) });
            }
        }
    }

    /// <summary>
    /// Draw the roll or pitch indicator: a short vertical block sliding either
    /// side of its dial's centre.
    /// </summary>
    /// <param name="offset">How far from centre, between -1 and 1.</param>
    /// <param name="origin">The dial's left-hand end, in screen coordinates.</param>
    private void DisplayIndicator(float offset, Vector2 origin)
    {
        float x = origin.X + (DialBarWidth / 2) + (offset * IndicatorTravel);

        for (int i = 0; i < IndicatorWidth; i++)
        {
            Surface.Graphics.DrawLine(new(x + i, origin.Y), new(x + i, origin.Y + DialBarHeight - 1), DialTopColor);
        }
    }

    private void DisplayMissiles(ScannerModel model)
    {
        Vector2 origin = MissilePosition;
        Vector2 location = ScannerRelative(
            origin with { X = origin.X + ((4 - model.Missiles.Count) * MissileSpacing) });

        foreach (MissileIndicator missile in model.Missiles)
        {
            Surface.Graphics.DrawImage(MissileImage(missile), location);
            location.X += MissileSpacing;
        }
    }

    /// <summary>
    /// Display the speed bar.
    /// </summary>
    private void DisplaySpeed(ScannerModel model)
    {
        Vector2 origin = ScannerRelative(SpeedPosition);
        float length = model.Speed * DialBarWidth;
        FastColor color = model.IsSpeedWarning ? SpeedWarningColor : DialTopColor;

        for (int i = 0; i < SpeedHeight; i++)
        {
            Surface.Graphics.DrawLine(new(origin.X, origin.Y + i), new(origin.X + length - 1, origin.Y + i), color);
        }
    }

    /// <summary>
    /// Update the compass which tracks the space station / planet.
    /// </summary>
    private void UpdateCompass(ScannerModel model)
    {
        if (model.Compass == null)
        {
            return;
        }

        CompassReading compass = model.Compass.Value;

        Vector2 position = ScannerRelative(new(
            CompassCentre.X - CompassDotRadius + (compass.Direction.X * CompassRadius) + 1f,
            CompassCentre.Y - CompassDotRadius + (compass.Direction.Y * -CompassRadius) + 1f));

        Surface.Graphics.DrawImage(
            compass.IsBehind ? nameof(ImageType.CompassRed) : nameof(ImageType.CompassGreen),
            position);
    }

    /// <summary>
    /// Update the scanner and draw all the lollipops. What is worth plotting
    /// is the controller's decision; how far off centre is too far is this
    /// tier's, since the two scanners are different sizes.
    /// </summary>
    private void UpdateScanner(ScannerModel model)
    {
        (float extentY, float extentX) = ScannerExtent;
        Vector2 centre = ScannerCentre;

        foreach (ScannerBlip blip in model.Blips)
        {
            if ((blip.BlipY < -extentY)
                || (blip.BlipY > extentY) ||
                (blip.X < -extentX)
                || (blip.X > extentX))
            {
                continue;
            }

            float x = blip.X + centre.X;
            float stickY = blip.StickY + centre.Y;
            float blipY = blip.BlipY + centre.Y;

            FastColor color = Ships.For(blip.Kind);

            // Both ends floored before the difference is taken, rather than
            // sizing the stick by its own fractional length: the drawing
            // floors a position and a height separately, so a stick reaching
            // up to the blip could floor a row short of it and leave a gap.
            float stickTop = MathF.Floor(MathF.Min(blipY, stickY));
            float stickBottom = MathF.Floor(MathF.Max(blipY, stickY));

            // ship
            Surface.Graphics.DrawRectangleFilled(new(x - 3, blipY), 5, 3, color);

            // stick
            Surface.Graphics.DrawRectangleFilled(new(x, stickTop), 2, stickBottom - stickTop, color);
        }
    }
}
