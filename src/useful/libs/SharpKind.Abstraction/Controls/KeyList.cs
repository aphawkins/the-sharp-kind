// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.ComponentModel;
using System.Text.Json.Serialization;

namespace SharpKind.Abstraction.Controls;

/// <summary>
/// The keys bound to one action. Written as a bare value when there is only
/// one of them, and as an array when there are several.
/// </summary>
/// <remarks>
/// <para>
/// Most actions have a single key, and <c>"FireLaser": "A"</c> reads better
/// than a one-item array - the file is meant to be edited by hand.
/// </para>
/// <para>
/// It needs a converter on each side because the file is read and written by
/// different libraries: <see cref="KeyListJsonConverter"/> writes it with
/// System.Text.Json, and <see cref="KeyListConverter"/> is what lets the
/// configuration binder read the bare form. Without the latter the binder
/// drops a bare value silently - it has a value and no children, so there is
/// nothing for it to bind a list from - and the binding would vanish with no
/// error at all.
/// </para>
/// <para>
/// The constructor is deliberately left implicit and public. The
/// configuration binder builds this itself for the array form, and an
/// internal one leaves it unable to - which loses every multi-key binding
/// while the single-key ones, built by the converter, still work.
/// </para>
/// </remarks>
[TypeConverter(typeof(KeyListConverter))]
[JsonConverter(typeof(KeyListJsonConverter))]
public sealed class KeyList : List<string>;
