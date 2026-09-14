// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.ComponentModel;
using System.Globalization;

namespace SharpKind.Abstraction.Controls;

/// <summary>
/// Reads a <see cref="KeyList"/> written as a bare value rather than an
/// array, which is the form a single key takes.
/// </summary>
/// <remarks>
/// For the configuration binder, which is what reads the file. A section
/// holding a value and no children has nothing for it to build a list from,
/// so without this the binding is dropped in silence; with it the binder
/// converts the value instead. The array form needs nothing here - the
/// binder builds that itself from the indexed children.
/// </remarks>
public sealed class KeyListConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        => sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        => value is string key ? new KeyList { key } : base.ConvertFrom(context, culture, value);
}
