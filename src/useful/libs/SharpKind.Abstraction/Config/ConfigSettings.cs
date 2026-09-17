// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

namespace SharpKind.Abstraction.Config;

/// <summary>
/// The part of a game's config file that is the same for every game: the schema
/// version and the shared engine settings under <c>engine</c>. It is separate
/// from <see cref="ConfigSettings{TGameSettings}"/> so code that only wants the
/// engine half - the composition root sizing a window before it knows what game
/// it is starting, say - can name a type without knowing the game's own
/// settings type. Games derive from the generic type below, not this one.
/// </summary>
public abstract class ConfigSettings
{
    /// <summary>
    /// Gets or sets the schema version of the file. It exists so a later rename or restructure
    /// can be migrated rather than silently reset; a file with no version reads as
    /// the current one, which is what every file written before versioning is.
    /// </summary>
    public int Version { get; set; } = ConfigSchema.CurrentVersion;

    public EngineConfigSettings Engine { get; set; } = new();

    /// <summary>
    /// Replaces any value that cannot be honoured with its default, in place,
    /// so one bad entry costs the user that entry rather than the whole file.
    /// Games override this to add their own settings, calling the base first.
    /// </summary>
    /// <returns><see langword="true"/> if anything had to be replaced.</returns>
    public virtual bool Repair()
    {
        bool repaired = false;

        // A future version means a later build wrote this file; nothing to migrate to, so it's stamped back and taken as read.
        if (Version < 1 || Version > ConfigSchema.CurrentVersion)
        {
            Version = ConfigSchema.CurrentVersion;
            repaired = true;
        }

        return Engine.Repair() || repaired;
    }
}
