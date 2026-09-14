// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Collections.ObjectModel;
using SharpKind.Abstraction.Config;

namespace SharpKind.Abstraction.Controls;

/// <summary>
/// What a game's controls file holds: the keyboard's bindings, and one entry
/// per controller the game knows how to drive.
/// </summary>
/// <remarks>
/// <para>
/// The shape is shared; what goes in it is not. Nothing here knows what an
/// action is called or which sticks exist - those are the game's, and they
/// arrive as the type arguments to <see cref="Repair{TAction, TAxis}"/> and
/// to <see cref="ControlMap{TAction, TAxis}"/>.
/// </para>
/// <para>
/// Written out in full the first time a game runs and read as the whole
/// answer afterwards, so editing it is how a binding changes and removing
/// one unbinds that control.
/// </para>
/// </remarks>
public sealed class ControlBindings
{
    /// <summary>
    /// Gets the name a controller entry uses to mean "anything not named by
    /// another entry". It is where a generic pad layout goes.
    /// </summary>
    public static string AnyDevice => "*";

    /// <summary>
    /// Gets or sets the schema version of the file, as a game's own config
    /// file carries one and for the same reason: a later rename or
    /// restructure can then be migrated rather than silently reset. A file
    /// with no version reads as the current one.
    /// </summary>
    public int Version { get; set; } = ConfigSchema.CurrentVersion;

    /// <summary>
    /// Gets the keys bound to each action, by action name. Populated in
    /// place by the reader, which is why it has no setter.
    /// </summary>
    public Dictionary<string, KeyList> Keyboard { get; } = [];

    /// <inheritdoc cref="Keyboard"/>
    public Collection<ControllerBindings> Controllers { get; } = [];

    /// <summary>
    /// Drops what cannot be honoured - a key or button the game does not
    /// know, an action it cannot perform, a controller entry naming no
    /// device - and restores the keyboard from <paramref name="defaults"/>
    /// if nothing of it survives.
    /// </summary>
    /// <remarks>
    /// Entry by entry, so one bad line costs that line rather than the whole
    /// file. An empty keyboard is the exception: it would leave the game
    /// unplayable on a machine with no controller.
    /// </remarks>
    /// <typeparam name="TAction">The game's bindable commands.</typeparam>
    /// <typeparam name="TAxis">The game's controls that take a position.</typeparam>
    /// <param name="defaults">
    /// What the game ships with, used only to put a keyboard back that the
    /// file has none of. Null accepts an empty one.
    /// </param>
    /// <returns><see langword="true"/> if anything had to be replaced.</returns>
    public bool Repair<TAction, TAxis>(ControlBindings? defaults)
        where TAction : struct, Enum
        where TAxis : struct, Enum
    {
        bool repaired = false;

        // A version from the future means the file was written by a later
        // build whose shape this one does not know. There is nothing to
        // migrate to, so it is stamped back and the bindings are taken as
        // read - the original is kept alongside either way.
        if (Version < 1 || Version > ConfigSchema.CurrentVersion)
        {
            Version = ConfigSchema.CurrentVersion;
            repaired = true;
        }

        foreach (string action in Keyboard.Keys.ToList())
        {
            if (!IsNamed<TAction>(action))
            {
                _ = Keyboard.Remove(action);
                repaired = true;
                continue;
            }

            if (Keyboard[action].RemoveAll(key => !Enum.TryParse<ConsoleKey>(key, ignoreCase: true, out _)) > 0)
            {
                repaired = true;
            }
        }

        if (Keyboard.Count == 0 && defaults?.Keyboard.Count > 0)
        {
            foreach ((string action, KeyList keys) in defaults.Keyboard)
            {
                Keyboard[action] = keys;
            }

            repaired = true;
        }

        foreach (ControllerBindings controller in Controllers.ToList())
        {
            if (string.IsNullOrWhiteSpace(controller?.Device))
            {
                _ = Controllers.Remove(controller!);
                repaired = true;
            }
        }

        foreach (ControllerBindings controller in Controllers)
        {
            repaired |= controller.Repair<TAction, TAxis>();
        }

        return repaired;
    }

    /// <summary>
    /// Whether the name is one of the game's, ignoring case so a file typed
    /// by hand need not match how the game spells it.
    /// </summary>
    /// <remarks>
    /// An enum's zero value means "nothing" and is never a binding, so a
    /// game's actions and axes must both reserve it. Without that, whichever
    /// control happened to be declared first would be dropped from every
    /// file as though the game had never heard of it.
    /// </remarks>
    /// <typeparam name="T">The game's actions, or its axes.</typeparam>
    /// <param name="name">The name read from the file.</param>
    /// <returns><see langword="true"/> if the game has a control by that name.</returns>
    internal static bool IsNamed<T>(string name)
        where T : struct, Enum
        => Enum.TryParse(name, ignoreCase: true, out T parsed)
            && !EqualityComparer<T>.Default.Equals(parsed, default);
}
