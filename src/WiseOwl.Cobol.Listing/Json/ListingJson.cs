// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Collections;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace WiseOwl.Cobol.Listing.Json;

/// <summary>
/// ⛔ THE ONE SERIALIZATION CONTRACT OF THE JSON LISTING (DESIGN-listing.md section 7.3). The writer
/// (<see cref="JsonListingWriter.Write"/>), the reader (<see cref="JsonListingWriter.Read"/>) and the schema generator
/// (<see cref="ListingJsonSchema"/>) all serialize through <see cref="Options"/>, so they see one set of properties, one
/// required set and one spelling of every enum value. It is the source-generated <see cref="ListingJsonContext"/> plus the
/// contract's three rules, each decided here once:
/// <list type="bullet">
/// <item>REQUIRED: a property whose value is never null is required (<see cref="IsRequired"/>), and the generator's
/// <c>required</c> lists are read from the same flag, so the reader refuses exactly the documents the schema calls
/// incomplete.</item>
/// <item>NO NULL ELEMENT: a list of the model never holds null; a document whose list does is refused.</item>
/// <item>ENUM VALUES ARE NAMES: a value is written and read as its member's name; a number is refused. A name this reader
/// does not know is refused in a document of this reader's minor or an earlier one (it is invalid there), and in a document
/// of a LATER minor, where the compatibility policy makes an added enum value a minor change, is read as
/// <see cref="UnknownEnumValue"/>.</item>
/// </list>
/// </summary>
public static class ListingJson
{
    /// <summary>The value an enum member of the model holds when a document of a later minor names a value this reader does
    /// not know (DESIGN-listing.md section 7.3): no member of any model enum has it, so <c>Enum.IsDefined</c> is false for
    /// it. Writing such a document again is refused, since the name it stood for is not known.</summary>
    public const int UnknownEnumValue = -1;

    /// <summary>The contract: the options the writer, the reader of a document of this reader's minor or earlier, and the
    /// schema generator use. Read-only.</summary>
    public static JsonSerializerOptions Options { get; } = Create(laterMinor: false);

    /// <summary>The contract for reading a document of a later minor than <see cref="ListingDocument.CurrentSchemaVersion"/>:
    /// the same, except that an enum value name it does not know reads as <see cref="UnknownEnumValue"/>.</summary>
    internal static JsonSerializerOptions LaterMinorOptions { get; } = Create(laterMinor: true);

    /// <summary>The contract's metadata of the document.</summary>
    public static JsonTypeInfo<ListingDocument> Document { get; } = DocumentOf(Options);

    internal static JsonTypeInfo<ListingDocument> LaterMinorDocument { get; } = DocumentOf(LaterMinorOptions);

    /// <summary>⛔ THE ONE RULE OF WHICH PROPERTIES ARE REQUIRED: a property whose value is never null. The writer leaves a
    /// null out, so a nullable property may be absent and is not required; every other one is always written and must be
    /// present. A property added in a later minor is therefore nullable, which keeps every older document valid.</summary>
    public static bool IsRequired(JsonPropertyInfo property) => !property.IsGetNullable;

    /// <summary>The minor of a schema version of major 1 (<c>1.&lt;minor&gt;</c>, the minor a decimal number without a
    /// leading zero), or null when <paramref name="schemaVersion"/> is no version of major 1.</summary>
    public static int? MinorOf(string? schemaVersion)
    {
        if (schemaVersion is null || !schemaVersion.StartsWith("1.", StringComparison.Ordinal)) return null;
        var minor = schemaVersion.AsSpan(2);
        if (minor.IsEmpty || minor.Length > 1 && minor[0] == '0') return null;
        foreach (char c in minor) if (c is < '0' or > '9') return null;
        return int.TryParse(minor, NumberStyles.None, CultureInfo.InvariantCulture, out int value) ? value : null;
    }

    private static JsonTypeInfo<ListingDocument> DocumentOf(JsonSerializerOptions options) =>
        (JsonTypeInfo<ListingDocument>)options.GetTypeInfo(typeof(ListingDocument));

    private static JsonSerializerOptions Create(bool laterMinor)
    {
        var options = new JsonSerializerOptions(ListingJsonContext.Default.Options)
        {
            TypeInfoResolver = ListingJsonContext.Default.WithAddedModifier(ApplyRules),
            AllowDuplicateProperties = false,
        };
        options.Converters.Add(new EnumNameConverterFactory(laterMinor));
        options.MakeReadOnly();
        return options;
    }

    /// <summary>The contract's REQUIRED and NO NULL ELEMENT rules, on each object type of the model.</summary>
    private static void ApplyRules(JsonTypeInfo type)
    {
        if (type.Kind != JsonTypeInfoKind.Object) return;
        foreach (var property in type.Properties) property.IsRequired = IsRequired(property);
        var lists = type.Properties.Where(p => ElementIsReference(p.PropertyType)).ToArray();
        if (lists.Length == 0) return;
        type.OnDeserialized = value =>
        {
            foreach (var list in lists)
                if (list.Get!(value) is IEnumerable elements)
                    foreach (object? element in elements)
                        if (element is null)
                            throw new JsonException($"a JSON listing's '{list.Name}' of {type.Type.Name} holds null, which no element of it may be");
        };
    }

    private static bool ElementIsReference(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) && !type.GetGenericArguments()[0].IsValueType;

    /// <summary>The contract's ENUM VALUES ARE NAMES rule, for every enum type of the model.</summary>
    private sealed class EnumNameConverterFactory(bool laterMinor) : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) =>
            typeToConvert.IsEnum && typeToConvert.Assembly == typeof(ListingDocument).Assembly;

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options) =>
            (JsonConverter)Activator.CreateInstance(typeof(EnumNameConverter<>).MakeGenericType(typeToConvert), [laterMinor])!;
    }

    private sealed class EnumNameConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        private static readonly Dictionary<string, T> ByName =
            Enum.GetValues<T>().ToDictionary(v => Enum.GetName(v)!, StringComparer.Ordinal);

        private static readonly Dictionary<T, string> NameOf = ByName.ToDictionary(p => p.Value, p => p.Key);

        private readonly bool _laterMinor;

        public EnumNameConverter(bool laterMinor)
        {
            if (typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false))
                throw new InvalidOperationException($"{typeof(T)} is a flags enum: a JSON listing's enum value is one member's name");
            if (Enum.IsDefined(typeof(T), UnknownEnumValue))
                throw new InvalidOperationException($"{typeof(T)} has a member of value {UnknownEnumValue}, which ListingJson.UnknownEnumValue reserves");
            _laterMinor = laterMinor;
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException($"a JSON listing's {typeof(T).Name} is the name of a value, not a {reader.TokenType}");
            string name = reader.GetString()!;
            if (ByName.TryGetValue(name, out var value)) return value;
            if (_laterMinor) return (T)Enum.ToObject(typeof(T), UnknownEnumValue);
            throw new JsonException($"'{name}' is no {typeof(T).Name} of schema version {ListingDocument.CurrentSchemaVersion} or earlier");
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options) =>
            writer.WriteStringValue(NameOf.TryGetValue(value, out var name) ? name
                : throw new JsonException($"{typeof(T).Name} value {value:D} is no member, so it has no name to write"));
    }
}
