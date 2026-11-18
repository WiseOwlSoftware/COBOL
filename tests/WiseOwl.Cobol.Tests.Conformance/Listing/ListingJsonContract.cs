// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Json;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance.Listing;

/// <summary>
/// The JSON contract every listing a test of this assembly produces is held to (DESIGN-listing.md section 10 item 5): its JSON
/// is valid against the committed schema, and it round-trips through <see cref="ListingJson"/> with no property the
/// model does not know. The listing tests' compile helpers call <see cref="AssertHolds"/> on every document they produce, so a
/// fact a later step adds to the model is checked against the schema by every listing test, not only by this one.
/// </summary>
internal static class ListingJsonContract
{
    private static readonly JsonNode Schema = JsonNode.Parse(ListingJsonSchema.Committed())!;

    private static readonly JsonSerializerOptions Strict = new(ListingJson.Options)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Assert <paramref name="document"/>'s JSON is valid against the schema and reads back, strictly, to itself.</summary>
    public static void AssertHolds(ListingDocument document)
    {
        string text = JsonListingWriter.Write(document);
        Assert.Empty(JsonSchemaSubset.Validate(Schema, JsonNode.Parse(text)));
        var read = (ListingDocument)JsonSerializer.Deserialize(text, Strict.GetTypeInfo(typeof(ListingDocument)))!;
        Assert.Equal(text, JsonListingWriter.Write(read));
    }
}
