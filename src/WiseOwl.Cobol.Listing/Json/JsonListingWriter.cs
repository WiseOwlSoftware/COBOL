// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using System.Text.Json;

namespace WiseOwl.Cobol.Listing.Json;

/// <summary>
/// The JSON listing (DESIGN-listing.md section 7.3): the whole <see cref="ListingDocument"/>, through the one contract
/// <see cref="ListingJson"/>. Like every renderer it reads only the model, so the JSON and the text listing of one
/// compilation describe one document. The document opens with <c>$schema</c> and <c>schemaVersion</c>
/// (<see cref="ListingDocument.Schema"/>, <see cref="ListingDocument.SchemaVersion"/>); the schema is
/// <c>listing-v1.schema.json</c> (<see cref="ListingJsonSchema"/>).
/// </summary>
public static class JsonListingWriter
{
    private const string SchemaVersionProperty = "schemaVersion";

    /// <summary>The JSON text of <paramref name="document"/>, ending in a line feed.</summary>
    /// <exception cref="JsonException">An enum member of the document holds a value that is no member (such as
    /// <see cref="ListingJson.UnknownEnumValue"/>, read from a later minor), which has no name to write.</exception>
    public static string Write(ListingDocument document) =>
        JsonSerializer.Serialize(document, ListingJson.Document) + "\n";

    /// <summary>
    /// Read a JSON listing written by any compiler of schema major 1 (DESIGN-listing.md section 7.3, the compatibility policy):
    /// a property this reader does not know is ignored, and in a document of a later minor an enum value it does not know reads
    /// as <see cref="ListingJson.UnknownEnumValue"/>, as the policy requires of every reader. Refused: a document of another
    /// schema or of another major, or with no <c>$schema</c> or <c>schemaVersion</c>; a property the schema requires that is
    /// absent, or null where the model has none (<see cref="ListingJson.IsRequired"/>); a null element of a list; an enum value
    /// written as a number, or one no version up to this reader's has, in a document of this reader's minor or earlier; and a
    /// property written twice.
    /// </summary>
    /// <exception cref="JsonException">The text is no JSON listing of schema major 1.</exception>
    public static ListingDocument Read(string json)
    {
        byte[] utf8 = Encoding.UTF8.GetBytes(json);
        string? version = SchemaVersionOf(utf8);
        int minor = ListingJson.MinorOf(version)
            ?? throw new JsonException(version is null ? $"a JSON listing names its {SchemaVersionProperty}"
                : $"a JSON listing of schema version '{version}' is not of major 1");
        var contract = minor > ListingJson.MinorOf(ListingDocument.CurrentSchemaVersion)
            ? ListingJson.LaterMinorDocument : ListingJson.Document;
        var document = JsonSerializer.Deserialize(utf8, contract)!;   // the text is an object (SchemaVersionOf)
        if (document.Schema != ListingDocument.SchemaUrn)
            throw new JsonException($"not a JSON listing of {ListingDocument.SchemaUrn}: $schema is '{document.Schema}'");
        return document;
    }

    /// <summary>The document's <c>schemaVersion</c>, read before the document so the contract that reads it is the one of its
    /// minor; null when the top-level object has no such string property (a duplicate is refused by the deserializer).</summary>
    /// <exception cref="JsonException">The text is no well-formed JSON.</exception>
    private static string? SchemaVersionOf(ReadOnlySpan<byte> utf8)
    {
        var reader = new Utf8JsonReader(utf8);
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"a JSON listing is an object, not a {reader.TokenType}");
        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            bool isVersion = reader.ValueTextEquals(SchemaVersionProperty);
            reader.Read();
            if (isVersion) return reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            reader.Skip();
        }
        return null;
    }
}
