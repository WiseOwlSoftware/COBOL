// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Nodes;
using System.Xml.Linq;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Json;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// ⛔ THE JSON LISTING'S SCHEMA CANNOT DRIFT FROM ITS WRITER (DESIGN-listing.md sections 7.3 and 10 item 5). The schema is
/// GENERATED from the one serialization contract (<see cref="ListingJson"/>) with the model's XML documentation as
/// its descriptions, and committed as <c>src/WiseOwl.Cobol.Listing/Json/listing-v1.schema.json</c>; this class regenerates it
/// and is red on any difference, so a property added, renamed or retyped in the model without the schema is caught here.
/// Re-generate with <c>WISEOWL_COBOL_UPDATE_LISTING_SCHEMA=1</c> (the case then writes the file and passes), read the diff,
/// and decide the version it needs (section 7.3: a property or enum value added is the next minor, one removed or renamed
/// the next major).
/// </summary>
public sealed class ListingJsonSchemaDriftTests
{
    private const string UpdateVariable = "WISEOWL_COBOL_UPDATE_LISTING_SCHEMA";

    private static readonly string CommittedPath = TestRepo.Src("WiseOwl.Cobol.Listing", "Json", ListingJsonSchema.FileName);

    /// <summary>The XML documentation file the Listing assembly is built with, beside it in this test's output.</summary>
    private static XDocument Documentation()
    {
        string path = Path.ChangeExtension(typeof(ListingDocument).Assembly.Location, ".xml");
        Assert.True(File.Exists(path), $"the Listing assembly's documentation file {path} is missing: the schema's descriptions are read from it");
        return XDocument.Load(path);
    }

    [Fact]
    public void TheCommittedSchema_IsTheSchemaTheContractGenerates()
    {
        string generated = ListingJsonSchema.Generate(Documentation());
        if (Environment.GetEnvironmentVariable(UpdateVariable) == "1")
        {
            File.WriteAllText(CommittedPath, generated);
            return;
        }
        string committed = File.ReadAllText(CommittedPath).ReplaceLineEndings("\n");
        Assert.True(committed == generated, $"{CommittedPath} is stale: it differs from the schema the contract generates at "
            + $"{FirstDifference(committed, generated)}. Re-generate it with {UpdateVariable}=1, read the diff, and bump "
            + $"ListingDocument.CurrentSchemaVersion as DESIGN-listing.md section 7.3 says.");
    }

    [Fact]
    public void TheEmbeddedSchema_IsTheCommittedFile() =>
        Assert.Equal(File.ReadAllText(CommittedPath).ReplaceLineEndings("\n"), ListingJsonSchema.Committed().ReplaceLineEndings("\n"));

    /// <summary>The schema names its dialect, its identifier (the document's <c>$schema</c> value) and the version it describes.</summary>
    [Fact]
    public void TheSchema_IdentifiesItself()
    {
        var schema = JsonNode.Parse(ListingJsonSchema.Committed())!.AsObject();
        Assert.Equal(ListingJsonSchema.Dialect, (string?)schema["$schema"]);
        Assert.Equal(ListingDocument.SchemaUrn, (string?)schema["$id"]);
        Assert.EndsWith(ListingDocument.CurrentSchemaVersion, (string?)schema["title"], StringComparison.Ordinal);
    }

    /// <summary>Every property at every depth carries a description: the schema states the meaning of everything it publishes.</summary>
    [Fact]
    public void EveryPropertyOfTheSchema_IsDescribed()
    {
        var undescribed = new List<string>();
        int properties = 0;
        void Walk(JsonNode? node, string path)
        {
            if (node is not JsonObject obj) return;
            if (obj["properties"] is JsonObject props)
                foreach (var (name, value) in props)
                {
                    properties++;
                    if (value is not JsonObject p || p["description"] is null) undescribed.Add(path + "." + name);
                    Walk(value, path + "." + name);
                }
            Walk(obj["items"], path + "[]");
        }
        Walk(JsonNode.Parse(ListingJsonSchema.Committed()), "$");
        Assert.True(properties > 0, "the schema has no properties");
        Assert.Empty(undescribed);
    }

    /// <summary>A member of the contract the documentation does not describe is refused by the generator, so no property is
    /// published without its meaning: the check is shown able to fail by dropping one member's documentation.</summary>
    [Fact]
    public void AnUndocumentedMember_IsRefused()
    {
        var docs = Documentation();
        docs.Descendants("member").Single(m => (string?)m.Attribute("name") == "F:WiseOwl.Cobol.Listing.ListingLineForm.Free").Remove();
        var e = Assert.Throws<InvalidOperationException>(() => ListingJsonSchema.Generate(docs));
        Assert.Contains("WiseOwl.Cobol.Listing.ListingLineForm.Free", e.Message, StringComparison.Ordinal);
    }

    /// <summary>Every document <see cref="JsonListingWriter"/> writes is valid against the committed schema, and the validator
    /// this class and the Conformance assembly use is shown able to refuse one (a value no enum holds).</summary>
    [Fact]
    public void AWrittenDocument_IsValid_AndAWrongEnumValueIsNot()
    {
        var schema = JsonNode.Parse(ListingJsonSchema.Committed())!;
        var written = JsonNode.Parse(JsonListingWriter.Write(JsonListingWriterTests.EveryMember()))!;
        Assert.Empty(JsonSchemaSubset.Validate(schema, written));
        written["lines"]![0]!["kind"] = "NoSuchKind";
        Assert.Contains(JsonSchemaSubset.Validate(schema, written), e => e.Contains("$.lines[0].kind", StringComparison.Ordinal));
        // A value the schema reaches through a $ref (the exporter writes one for a type it has written already) is checked too.
        written["statistics"]!["files"]![0]!["lines"]!["kinds"]![0]!["kind"] = "NoSuchKind";
        Assert.Contains(JsonSchemaSubset.Validate(schema, written),
            e => e.Contains("$.statistics.files[0].lines.kinds[0].kind", StringComparison.Ordinal));
    }

    private static string FirstDifference(string a, string b)
    {
        string[] x = a.Split('\n'), y = b.Split('\n');
        for (int i = 0; i < Math.Max(x.Length, y.Length); i++)
            if (i >= x.Length || i >= y.Length || x[i] != y[i])
                return $"line {i + 1} (committed: {(i < x.Length ? x[i].Trim() : "<end>")} | generated: {(i < y.Length ? y[i].Trim() : "<end>")})";
        return "no line";
    }
}
