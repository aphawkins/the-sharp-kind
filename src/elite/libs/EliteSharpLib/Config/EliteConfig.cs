// 'Elite - The Sharp Kind' - Andy Hawkins 2023-2026.
// 'Elite - The New Kind' - C.J.Pinder 1999-2001.
// Elite (C) I.Bell & D.Braben 1984.

using EliteSharpLib.Planets;
using EliteSharpLib.Suns;
using SharpKind.Abstraction.Config;
using SharpKind.Abstraction.Renditions;

namespace EliteSharpLib.Config;

// The root of elite.sharp: shared engine settings plus Elite's own.
internal sealed class EliteConfig : ConfigSettings<EliteConfigSettings>
{
    // The shared engine default is 16-bit, chosen for no reason particular to
    // this game; Elite's own commander-facing default is the 8-bit tier,
    // which is also the rendition whose own DefaultWindowScale (4) then
    // applies to a commander who has never chosen a scale either.
    public EliteConfig()
    {
        Engine.Rendition = RenditionNames.EightBit;
        Engine.FallbackRendition = RenditionNames.EightBit;
    }

    public override bool Repair()
    {
        // The base repairs the engine half and the version; this adds Elite's
        // own. Deliberately not short-circuiting: every setting is checked.
        bool repaired = base.Repair();

        if (!Enum.IsDefined(Game.PlanetStyle))
        {
            Game.PlanetStyle = PlanetType.Fractal;
            repaired = true;
        }

        if (!Enum.IsDefined(Game.SunStyle))
        {
            Game.SunStyle = SunType.Gradient;
            repaired = true;
        }

        if (!Enum.IsDefined(Game.PlanetDescriptions))
        {
            Game.PlanetDescriptions = PlanetDescriptions.TreeGrubs;
            repaired = true;
        }

        return repaired;
    }
}
