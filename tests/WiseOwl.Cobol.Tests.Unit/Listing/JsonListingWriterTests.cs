// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.Json.Nodes;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Json;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The JSON listing's contract (DESIGN-listing.md section 7.3): camelCase names, enums as their member names, a null left out,
/// the document opening with <c>$schema</c> and <c>schemaVersion</c>, and the compatibility policy's reader: a property it does
/// not know is ignored, a document of another schema or major is refused. Each expected value is read off the hand-built
/// document below (the input), never off a captured run.
/// </summary>
public sealed class JsonListingWriterTests
{
    private static readonly DateTimeOffset Instant = new(2026, 11, 17, 14, 3, 22, TimeSpan.FromHours(-5));

    /// <summary>A document holding every member of every record, each nullable member once present and once absent, so a
    /// property the writer or the reader mishandles has a value here to be lost.</summary>
    internal static ListingDocument EveryMember() => new(
        new ListingHeader("30.0.0", 85, "COBOL-85", Instant, "main.cob", ListingOutcome.BindError),
        [new ListingFile(1, "main.cob", ListingFileRole.Source, new string('a', 64)),
         new ListingFile(2, "lib/REC.cpy", ListingFileRole.Library, new string('b', 64))],
        [new ListingInclusion(1, null, 1, 2, 2, "REC", "LIB", 2, ListingInclusionOutcome.Found),
         new ListingInclusion(2, 1, 2, 3, 3, "\"gone.cpy\"", null, null, ListingInclusionOutcome.NotFound)],
        [new ListingLine(1, 1, 1, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "000100 ID DIVISION.", false, true,
            false, 0, ListingReplacement.None, null),
         new ListingLine(2, 1, 2, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "000200 DATE-COMPILED. X.", false, true,
            true, 0, ListingReplacement.DateCompiled, "DATE-COMPILED. 2026-11-17 14:03:22 -05:00."),
         new ListingLine(3, 2, 1, 1, 1, ListingLineForm.Free, ListingLineKind.PageEjectComment, "*> été", true, false,
            false, null, ListingReplacement.DateCompiledEntry, null)],
        [new ListingProgram(0, "MAIN", ListingProgramKind.Program, null, 0, 1, 3),
         new ListingProgram(1, "INNER", ListingProgramKind.Program, 0, 1, 2, 3)],
        [new ListingSectionStatus(ListingSection.Source, true, null),
         new ListingSectionStatus(ListingSection.Programs, false, "the bind refused the program")]);

    private static JsonObject Written(ListingDocument document) => JsonNode.Parse(JsonListingWriter.Write(document))!.AsObject();

    [Fact]
    public void TheDocument_OpensWithItsSchemaAndVersion()
    {
        var json = Written(EveryMember());
        Assert.Equal(["$schema", "schemaVersion", "header", "files", "inclusions", "lines", "programs", "sections"],
            json.Select(p => p.Key));
        Assert.Equal("urn:wiseowl:cobol:listing:v1", (string?)json["$schema"]);
        Assert.Equal("1.0", (string?)json["schemaVersion"]);
    }

    [Fact]
    public void NamesAreCamelCase_EnumsAreTheirNames_AndANullIsLeftOut()
    {
        var json = Written(EveryMember());
        Assert.Equal("BindError", (string?)json["header"]!["outcome"]);
        Assert.Equal(85, (int?)json["header"]!["edition"]);
        Assert.Equal("2026-11-17T14:03:22-05:00", (string?)json["header"]!["compiledAt"]);
        var copy = json["inclusions"]![0]!.AsObject();
        Assert.False(copy.ContainsKey("parent"));                 // null: left out
        Assert.Equal("LIB", (string?)copy["libraryName"]);
        var missing = json["inclusions"]![1]!.AsObject();
        Assert.Equal(1, (int?)missing["parent"]);
        Assert.False(missing.ContainsKey("libraryName"));
        Assert.False(missing.ContainsKey("file"));
        Assert.Equal("NotFound", (string?)missing["outcome"]);
        var replaced = json["lines"]![1]!.AsObject();
        Assert.Equal("DateCompiled", (string?)replaced["replacement"]);
        Assert.Equal("DATE-COMPILED. 2026-11-17 14:03:22 -05:00.", (string?)replaced["replaced"]);
        Assert.True((bool?)replaced["pageBreakBefore"]);
        var copied = json["lines"]![2]!.AsObject();
        Assert.Equal("*> été", (string?)copied["text"]);   // the text as the file holds it, whatever its characters
        Assert.False(copied.ContainsKey("program"));
        Assert.Equal("PageEjectComment", (string?)copied["kind"]);
        Assert.Equal("the bind refused the program", (string?)json["sections"]![1]!["reason"]);
        Assert.False(json["sections"]![0]!.AsObject().ContainsKey("reason"));
    }

    [Fact]
    public void TheText_IsIndented_WithLineFeeds_AndEndsInOne()
    {
        string text = JsonListingWriter.Write(EveryMember());
        Assert.DoesNotContain('\r', text);
        Assert.StartsWith("{\n  \"$schema\": ", text, StringComparison.Ordinal);
        Assert.EndsWith("}\n", text, StringComparison.Ordinal);
    }

    /// <summary>What is written reads back to the same document: every property the model has is in the JSON, and nothing in
    /// the JSON is unknown to the model (the reading below refuses an unknown property).</summary>
    [Fact]
    public void ADocument_RoundTrips_WithNoUnknownProperty()
    {
        var document = EveryMember();
        string text = JsonListingWriter.Write(document);
        var strict = new JsonSerializerOptions(ListingJsonContext.Default.Options)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };
        var read = (ListingDocument)JsonSerializer.Deserialize(text, strict.GetTypeInfo(typeof(ListingDocument)))!;
        Assert.Equal(text, JsonListingWriter.Write(read));
        Assert.Equal(text, JsonListingWriter.Write(JsonListingWriter.Read(text)));
        Assert.Equal(document.Header, read.Header);
        Assert.Equal(document.Lines, read.Lines);
    }

    /// <summary>The compatibility policy: within a major every addition is optional and a reader ignores what it does not know.</summary>
    [Fact]
    public void TheReader_IgnoresAPropertyItDoesNotKnow()
    {
        var json = Written(EveryMember());
        json["schemaVersion"] = "1.7";
        json["aSectionOfALaterMinor"] = new JsonArray(1, 2);
        json["lines"]![0]!["aPropertyOfALaterMinor"] = "x";
        var read = JsonListingWriter.Read(json.ToJsonString());
        Assert.Equal("1.7", read.SchemaVersion);
        Assert.Equal(EveryMember().Lines, read.Lines);
    }

    [Theory]
    [InlineData("$schema", "urn:someone:else")]
    [InlineData("schemaVersion", "2.0")]
    public void TheReader_RefusesAnotherSchemaOrMajor(string property, string value)
    {
        var json = Written(EveryMember());
        json[property] = value;
        Assert.Throws<JsonException>(() => JsonListingWriter.Read(json.ToJsonString()));
    }

    [Theory]
    [InlineData("$schema")]
    [InlineData("schemaVersion")]
    public void TheReader_RefusesADocumentThatDoesNotNameItsSchema(string property)
    {
        var json = Written(EveryMember());
        json.Remove(property);
        Assert.Throws<JsonException>(() => JsonListingWriter.Read(json.ToJsonString()));
    }

    /// <summary>Nullable annotations are respected: a null where the model has none is refused, not read as a null string.</summary>
    [Fact]
    public void TheReader_RefusesANullTheModelDoesNotAllow()
    {
        var json = Written(EveryMember());
        json["lines"]![0]!["text"] = null;
        Assert.Throws<JsonException>(() => JsonListingWriter.Read(json.ToJsonString()));
    }
}
