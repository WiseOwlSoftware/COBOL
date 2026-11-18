// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using System.Text.Json.Nodes;
using WiseOwl.Cobol.Listing;
using WiseOwl.Cobol.Listing.Json;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit.Listing;

/// <summary>
/// The JSON listing's contract (DESIGN-listing.md section 7.3, <see cref="ListingJson"/>): camelCase names, enums as their
/// member names, a null left out, the document opening with <c>$schema</c> and <c>schemaVersion</c>, and the compatibility
/// policy's reader: a property it does not know is ignored, and so is an enum value it does not know in a later minor; a
/// document of another schema or major, or one the schema calls invalid (a required property missing, a null where none is
/// allowed, an enum value written as a number), is refused. Each expected value is read off the hand-built
/// document below (the input), never off a captured run.
/// </summary>
public sealed class JsonListingWriterTests
{
    private static readonly DateTimeOffset Instant = new(2026, 11, 17, 14, 3, 22, TimeSpan.FromHours(-5));

    /// <summary>A document holding every member of every record, each nullable member once present and once absent, so a
    /// property the writer or the reader mishandles has a value here to be lost.</summary>
    internal static ListingDocument EveryMember() => new(
        new ListingHeader("30.0.0", 85, "COBOL-85", Instant, "main.cob", ListingOutcome.BindError, ListingParse.Parsed),
        [new ListingFile(1, "main.cob", ListingFileRole.Source, new string('a', 64)),
         new ListingFile(2, "lib/REC.cpy", ListingFileRole.Library, new string('b', 64))],
        [new ListingInclusion(1, null, 1, 2, 2, "REC", "LIB", 2, ListingInclusionOutcome.Found, false),
         new ListingInclusion(2, 1, 2, 3, 3, "\"gone.cpy\"", null, null, ListingInclusionOutcome.NotFound, true)],
        [new ListingLine(1, 1, 1, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "000100 ID DIVISION.", false, true,
            false, 0, ListingReplacement.None, null, [1], new ListingMargin(1, 0, 5, "DISPLAY")),
         new ListingLine(2, 1, 2, null, 0, ListingLineForm.Fixed, ListingLineKind.Source, "000200 DATE-COMPILED. X.", false, true,
            true, 0, ListingReplacement.DateCompiled, "DATE-COMPILED. 2026-11-17 14:03:22 -05:00.", [], null),
         new ListingLine(3, 2, 1, 1, 1, ListingLineForm.Free, ListingLineKind.PageEjectComment, "*> été", true, false,
            false, null, ListingReplacement.DateCompiledEntry, null, [], null),
         new ListingLine(4, 1, 3, null, 0, ListingLineForm.Free, ListingLineKind.Source, "MOVE A TO B.", false, true,
            false, 0, ListingReplacement.Replacing, "MOVE X TO B.\nDISPLAY B.", [], null)],
        [new ListingProgram(0, "MAIN", ListingProgramKind.Program, null, 0, 1, 3,
             new ListingProgramAttributes(true, false, true, false, "main-ext", "MAIN")),
         new ListingProgram(1, "INNER", ListingProgramKind.Program, 0, 1, 2, 3, null)],
        [new ListingDiagnostic(1, "COBOL0001", ListingSeverity.Error, "no (ISO §2023:14.9.8.4 GR1)", 1, 1, 1, 8,
            [new ListingCitation(2023, "14.9.8.4", "GR1"), new ListingCitation(1985, "VI.5.3", null)]),
         new ListingDiagnostic(2, "CS0103", ListingSeverity.Warning, "generated", null, null, null, null, [])],
        [new ListingDisplay(4, "HELLO 42")],
        [new ListingOption("--std", "85 (ANSI X3.23-1985)", ListingOptionOrigin.Specified),
         new ListingOption("--permissive", "off", ListingOptionOrigin.Default),
         new ListingOption("compiler", "30.0.0", ListingOptionOrigin.Host)],
        new ListingStatistics(
            new ListingLineCounts(4, [new ListingKindCount(ListingLineKind.Source, 3), new ListingKindCount(ListingLineKind.PageEjectComment, 1)], 1, 1, 1),
            [new ListingFileStatistics(1, new ListingLineCounts(3, [new ListingKindCount(ListingLineKind.Source, 3)], 0, 0, 0))],
            [new ListingSeverityCount(ListingSeverity.Error, 1), new ListingSeverityCount(ListingSeverity.Warning, 1)],
            new ListingParseStatistics(2, 3, [new ListingVerb("MOVE", 2, [4, 4]), new ListingVerb("STOP", 1, [])])),
        new ListingDataMap(
            [new ListingDataItem(1, ListingDataItemKind.Data, "REC", 0, 1, 8, 1, null, "WORKING-STORAGE", null, null, "GROUP", 1, 3, 2,
                [3], null, null, null, true, false, true, 0, 15, 15, "REC", "_T_1[]", []),
             new ListingDataItem(2, ListingDataItemKind.Data, "N", null, null, null, 66, 1, "OTHER", "DISPLAY", "9(5)", "NUMERIC",
                null, null, null, [], 1, 1, 1, false, true, false, null, 5, 5, null, null, []),
             new ListingDataItem(3, ListingDataItemKind.Index, "IX", 0, 1, 20, null, 1, null, null, null, null, null, null, null, [],
                null, null, null, false, false, false, null, null, null, "_IX_1", "long", []),
             new ListingDataItem(4, ListingDataItemKind.Condition, "YES", 0, 2, 12, 88, 2, null, null, null, null, null, null, null, [],
                null, null, null, false, false, false, null, null, null, null, null, ["\"Y\"", "1 THRU 9"]),
             new ListingDataItem(5, ListingDataItemKind.Constant, "K", 0, 3, 8, null, null, null, null, null, null, null, null, null, [],
                null, null, null, true, false, false, null, null, null, null, null, ["5"])],
            [new ListingFileEntry(1, "F", 0, 1, 12, false, "INDEXED", "DYNAMIC", [1], true, false),
             new ListingFileEntry(2, "S", null, null, null, true, "SEQUENTIAL", "SEQUENTIAL", [], false, true)]),
        [new ListingSectionStatus(ListingSection.Source, true, null),
         new ListingSectionStatus(ListingSection.Programs, false, "the bind refused the program"),
         new ListingSectionStatus(ListingSection.Diagnostics, true, null),
         new ListingSectionStatus(ListingSection.Options, true, null),
         new ListingSectionStatus(ListingSection.Statistics, true, null),
         new ListingSectionStatus(ListingSection.DataMap, true, null)]);

    /// <summary>Each line as a value: a record's list member compares by reference, so a line's diagnostic ids are compared as a
    /// sequence beside the rest of it.</summary>
    private static List<(ListingLine Line, string Diagnostics)> ByValue(IEnumerable<ListingLine> lines) =>
        [.. lines.Select(l => (l with { Diagnostics = NoIds }, string.Join(",", l.Diagnostics)))];

    private static readonly int[] NoIds = [];

    private static JsonObject Written(ListingDocument document) => JsonNode.Parse(JsonListingWriter.Write(document))!.AsObject();

    [Fact]
    public void TheDocument_OpensWithItsSchemaAndVersion()
    {
        var json = Written(EveryMember());
        Assert.Equal(["$schema", "schemaVersion", "header", "files", "inclusions", "lines", "programs", "diagnostics", "displays",
            "options", "statistics", "dataMap", "sections"], json.Select(p => p.Key));
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
        Assert.False((bool?)copy["suppress"]);                    // a false is written: the member is never null
        Assert.True((bool?)missing["suppress"]);
        var changed = json["lines"]![3]!.AsObject();
        Assert.Equal("Replacing", (string?)changed["replacement"]);
        Assert.Equal("MOVE X TO B.\nDISPLAY B.", (string?)changed["replaced"]);
        var display = Assert.Single(json["displays"]!.AsArray())!.AsObject();
        Assert.Equal(4, (int?)display["line"]);
        Assert.Equal("HELLO 42", (string?)display["text"]);
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
        var strict = new JsonSerializerOptions(ListingJson.Options)
        {
            UnmappedMemberHandling = System.Text.Json.Serialization.JsonUnmappedMemberHandling.Disallow,
        };
        var read = (ListingDocument)JsonSerializer.Deserialize(text, strict.GetTypeInfo(typeof(ListingDocument)))!;
        Assert.Equal(text, JsonListingWriter.Write(read));
        Assert.Equal(text, JsonListingWriter.Write(JsonListingWriter.Read(text)));
        Assert.Equal(document.Header, read.Header);
        Assert.Equal(ByValue(document.Lines), ByValue(read.Lines));
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
        Assert.Equal(ByValue(EveryMember().Lines), ByValue(read.Lines));
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

    /// <summary>The policy's other half (review R1-2): an enum value added in a later minor is read, as
    /// <see cref="ListingJson.UnknownEnumValue"/>, which no member has; the same name in a document of this reader's minor is
    /// invalid and refused; and the read document cannot be written again, since the value's name is not known.</summary>
    [Fact]
    public void TheReader_ReadsAnEnumValueOfALaterMinor_AsTheUnknownValue()
    {
        var json = Written(EveryMember());
        json["schemaVersion"] = "1.1";
        json["lines"]![0]!["kind"] = "AKindOfALaterMinor";
        json["sections"]![1]!["section"] = "ASectionOfALaterMinor";
        var read = JsonListingWriter.Read(json.ToJsonString());
        Assert.Equal((ListingLineKind)ListingJson.UnknownEnumValue, read.Lines[0].Kind);
        Assert.False(Enum.IsDefined(read.Lines[0].Kind));
        Assert.Equal((ListingSection)ListingJson.UnknownEnumValue, read.Sections[1].Section);
        Assert.Equal(ByValue([EveryMember().Lines[1]]), ByValue([read.Lines[1]]));
        Assert.Throws<JsonException>(() => JsonListingWriter.Write(read));

        json["schemaVersion"] = ListingDocument.CurrentSchemaVersion;
        Assert.Contains("AKindOfALaterMinor", Assert.Throws<JsonException>(() => JsonListingWriter.Read(json.ToJsonString())).Message,
            StringComparison.Ordinal);
    }

    /// <summary>An enum value is its member's name, never a number (the schema's <c>enum</c> lists names only), in a document of
    /// any minor.</summary>
    [Theory]
    [InlineData("1.0")]
    [InlineData("1.9")]
    public void TheReader_RefusesAnEnumValueWrittenAsANumber(string version)
    {
        var json = Written(EveryMember());
        json["schemaVersion"] = version;
        json["lines"]![0]!["kind"] = 1;
        Assert.Throws<JsonException>(() => JsonListingWriter.Read(json.ToJsonString()));
    }

    /// <summary>No model enum has a member of <see cref="ListingJson.UnknownEnumValue"/>, so the value read for a later minor's
    /// enum value is never a member's.</summary>
    [Fact]
    public void NoModelEnum_HasTheUnknownValue()
    {
        var enums = typeof(ListingDocument).Assembly.GetExportedTypes()
            .Where(t => t.IsEnum && t.Namespace == typeof(ListingDocument).Namespace && t != typeof(ListingFormats)).ToList();
        Assert.Contains(typeof(ListingLineKind), enums);
        Assert.All(enums, t => Assert.False(Enum.IsDefined(t, ListingJson.UnknownEnumValue), t.Name));
    }

    /// <summary>⛔ THE READER REFUSES EXACTLY WHAT THE SCHEMA CALLS INCOMPLETE (review R1-1): every property of the written
    /// document, at every depth, is removed in turn; the reading is refused when the committed schema lists it as
    /// <c>required</c> there and accepted when it does not. The schema's <c>required</c> lists and the reader both come from
    /// <see cref="ListingJson.IsRequired"/>, and this holds them together from the outside.</summary>
    [Fact]
    public void TheReader_RefusesADocumentMissingARequiredProperty_AndOnlyThat()
    {
        var schema = JsonNode.Parse(ListingJsonSchema.Committed())!.AsObject();
        var cases = new List<(List<object> Parent, string Name, bool Required)>();
        void Walk(JsonObject node, JsonNode? instance, List<object> path)
        {
            if (node["properties"] is JsonObject properties && instance is JsonObject obj)
            {
                var required = (node["required"] as JsonArray ?? []).Select(n => (string)n!).ToHashSet(StringComparer.Ordinal);
                foreach (var (name, property) in properties)
                {
                    if (!obj.ContainsKey(name)) continue;
                    cases.Add((path, name, required.Contains(name)));
                    if (property is JsonObject p) Walk(p, obj[name], [.. path, name]);
                }
            }
            if (node["items"] is JsonObject items && instance is JsonArray array)
                for (int i = 0; i < array.Count; i++) Walk(items, array[i], [.. path, i]);
        }
        Walk(schema, Written(EveryMember()), []);
        Assert.Contains(cases, c => c.Required && c.Parent.Count == 0 && c.Name == "header");
        Assert.Contains(cases, c => c.Required && c.Name == "text");
        Assert.Contains(cases, c => c.Required && c.Name == "compilerVersion");
        Assert.Contains(cases, c => !c.Required && c.Name == "libraryName");

        var wrong = new List<string>();
        foreach (var (parent, name, required) in cases)
        {
            JsonNode node = Written(EveryMember());
            foreach (var step in parent) node = step is int i ? node[i]! : node[(string)step]!;
            node.AsObject().Remove(name);
            string where = string.Join(".", parent) + "." + name;
            try
            {
                JsonListingWriter.Read(node.Root.ToJsonString());
                if (required) wrong.Add(where + " (required) was read without it");
            }
            catch (JsonException e)
            {
                if (!required) wrong.Add(where + " (optional) was refused: " + e.Message);
            }
        }
        Assert.Empty(wrong);
    }

    /// <summary>A list of the model never holds null (the schema's items are objects), so a document whose list does is refused.</summary>
    [Theory]
    [InlineData("files")]
    [InlineData("lines")]
    [InlineData("sections")]
    public void TheReader_RefusesANullElementOfAList(string list)
    {
        var json = Written(EveryMember());
        json[list]!.AsArray().Add(null);
        Assert.Throws<JsonException>(() => JsonListingWriter.Read(json.ToJsonString()));
    }

    [Fact]
    public void TheReader_RefusesAPropertyWrittenTwice()
    {
        string text = JsonListingWriter.Write(EveryMember());
        string twice = text.Replace("\"schemaVersion\": \"1.0\",", "\"schemaVersion\": \"1.0\", \"$schema\": \"urn:wiseowl:cobol:listing:v1\",",
            StringComparison.Ordinal);
        Assert.NotEqual(text, twice);
        Assert.Throws<JsonException>(() => JsonListingWriter.Read(twice));
    }

    /// <summary>The one parser of a schema version of major 1: <c>1.</c> and a decimal minor with no leading zero.</summary>
    [Theory]
    [InlineData("1.0", 0)]
    [InlineData("1.7", 7)]
    [InlineData("1.12", 12)]
    [InlineData("1.", null)]
    [InlineData("1.x", null)]
    [InlineData("1.01", null)]
    [InlineData("1.-1", null)]
    [InlineData("1.1 ", null)]
    [InlineData("2.0", null)]
    [InlineData("1", null)]
    [InlineData(null, null)]
    public void TheMinor_IsReadFromASchemaVersionOfMajor1(string? version, int? minor)
    {
        Assert.Equal(minor, ListingJson.MinorOf(version));
        if (version is null || minor is not null) return;
        var json = Written(EveryMember());
        json["schemaVersion"] = version;
        Assert.Throws<JsonException>(() => JsonListingWriter.Read(json.ToJsonString()));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{")]
    public void TheReader_RefusesATextThatIsNoObject(string text) =>
        Assert.ThrowsAny<JsonException>(() => JsonListingWriter.Read(text));
}
