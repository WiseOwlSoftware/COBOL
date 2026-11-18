// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;

namespace WiseOwl.Cobol.Listing.Json;

/// <summary>
/// The JSON listing (DESIGN-listing.md section 7.3): the whole <see cref="ListingDocument"/>, through the one contract
/// <see cref="ListingJsonContext"/>. Like every renderer it reads only the model, so the JSON and the text listing of one
/// compilation describe one document. The document opens with <c>$schema</c> and <c>schemaVersion</c>
/// (<see cref="ListingDocument.Schema"/>, <see cref="ListingDocument.SchemaVersion"/>); the schema is
/// <c>listing-v1.schema.json</c> (<see cref="ListingJsonSchema"/>).
/// </summary>
public static class JsonListingWriter
{
    /// <summary>The JSON text of <paramref name="document"/>, ending in a line feed.</summary>
    public static string Write(ListingDocument document) =>
        JsonSerializer.Serialize(document, ListingJsonContext.Default.ListingDocument) + "\n";

    /// <summary>
    /// Read a JSON listing written by any compiler of schema major 1 (DESIGN-listing.md section 7.3, the compatibility policy):
    /// a property this reader does not know is ignored, as the policy requires of every reader; a document of another schema,
    /// or of another major, is refused, as is a null where the model has none.
    /// </summary>
    /// <exception cref="JsonException">The text is no JSON listing of schema major 1.</exception>
    public static ListingDocument Read(string json)
    {
        var document = JsonSerializer.Deserialize(json, ListingJsonContext.Default.ListingDocument)
            ?? throw new JsonException("a JSON listing is an object, not null");
        if (document.Schema != ListingDocument.SchemaUrn)
            throw new JsonException($"not a JSON listing of {ListingDocument.SchemaUrn}: $schema is '{document.Schema}'");
        if (!document.SchemaVersion.StartsWith("1.", StringComparison.Ordinal))
            throw new JsonException($"a JSON listing of schema version '{document.SchemaVersion}' is not of major 1");
        return document;
    }
}
