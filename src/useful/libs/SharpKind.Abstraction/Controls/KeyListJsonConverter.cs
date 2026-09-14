// 'SharpKind Libraries' - Andy Hawkins 2023-2026.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace SharpKind.Abstraction.Controls;

/// <summary>
/// Writes a <see cref="KeyList"/> as a bare value when it holds one key, and
/// as an array when it holds several. Reads either.
/// </summary>
/// <remarks>
/// The writing half is what this is for: the file is written with
/// System.Text.Json, and a one-item array is noise in something meant to be
/// edited by hand. The reading half is here because a converter has to have
/// one, and because it keeps the type usable anywhere the file is parsed
/// with System.Text.Json rather than the configuration binder - the tests
/// do exactly that.
/// </remarks>
public sealed class KeyListJsonConverter : JsonConverter<KeyList>
{
    public override KeyList Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return [reader.GetString() ?? string.Empty];
        }

        KeyList keys = [];

        if (reader.TokenType != JsonTokenType.StartArray)
        {
            return keys;
        }

        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                keys.Add(reader.GetString() ?? string.Empty);
            }
        }

        return keys;
    }

    public override void Write(Utf8JsonWriter writer, KeyList value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        if (value.Count == 1)
        {
            writer.WriteStringValue(value[0]);
            return;
        }

        writer.WriteStartArray();

        foreach (string key in value)
        {
            writer.WriteStringValue(key);
        }

        writer.WriteEndArray();
    }
}
