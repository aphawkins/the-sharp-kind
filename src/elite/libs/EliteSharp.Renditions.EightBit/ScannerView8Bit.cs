// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using System.Numerics;
using EliteSharp.Abstractions.Views;
using SharpKind;

namespace EliteSharp.Renditions.EightBit;

/// <summary>
/// The 8-bit HUD, laid out against the 320x56 scanner bitmap. Roughly the
/// 16-bit offsets halved, since the scanner is a little under half the size in
/// each axis - first drafts against the art rather than authored positions.
/// </summary>
internal sealed class ScannerView8Bit : ScannerViewBase
{
    internal ScannerView8Bit(IViewSurface surface, ShipColours ships)
        : base(surface)
    {
        Ships = ships;

        DialTopColor = surface.Palette["Yellow"];
        DialBodyColor = surface.Palette["Orange"];
        DialBottomColor = surface.Palette["Red"];
        SpeedWarningColor = surface.Palette["Red"];
    }

    protected override Vector2 ScannerCentre => new(Surface.Layout.ViewportCentre.X - 2, Surface.Layout.ViewportHeight + 28);

    protected override (float Y, float X) ScannerExtent => (14, 25);

    protected override int DialBarHeight => 4;

    protected override float DialBarWidth => 32;

    protected override float IndicatorTravel => 14;

    protected override int IndicatorWidth => 2;

    protected override Vector2 ShieldFrontPosition => new(18, 2);

    protected override Vector2 ShieldRearPosition => new(18, 10);

    protected override Vector2 FuelPosition => new(18, 18);

    protected override Vector2 CabinTempPosition => new(18, 26);

    protected override Vector2 LaserTempPosition => new(18, 33);

    protected override Vector2 AltitudePosition => new(18, 42);

    protected override Vector2 EnergyPosition => new(270, 26);

    protected override float EnergyBankSpacing => 8;

    protected override Vector2 SpeedPosition => new(270, 2);

    protected override float SpeedHeight => 4;

    // Left-aligned with the speed and energy dials: the indicators centre on half a dial's width in.
    protected override Vector2 RollPosition => new(270, 10);

    protected override Vector2 PitchPosition => new(270, 18);

    protected override Vector2 CompassCentre => new(257, 10);

    protected override float CompassRadius => 8;

    protected override float CompassDotRadius => 2;

    protected override Vector2 MissilePosition => new(18, 49);

    protected override float MissileSpacing => 8;

    protected override Vector2 StationIndicatorPosition => new(255, 35);

    protected override Vector2 EcmIndicatorPosition => new(72, 46);

    protected override FastColor DialTopColor { get; }

    protected override FastColor DialBodyColor { get; }

    protected override FastColor DialBottomColor { get; }

    protected override FastColor SpeedWarningColor { get; }

    protected override ShipColours Ships { get; }
}
