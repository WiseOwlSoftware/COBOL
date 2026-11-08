// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json.Nodes;
using WiseOwl.Cobol.Runtime.Messaging;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The <c>servers</c> member of <c>mcs.json</c> (docs/rearchitecture/DESIGN-message-control.md §0 Q3, Q8, §5.4.5; kb/Work PB4745). The standard leaves how a message server is named,
/// found and started to the implementor (§0 Q3 gives the answer), so the expected values here are the design's: a name compares with trailing spaces removed and without regard to
/// case, <c>maxLength</c> is a positive whole number, <c>start</c> is a command line or an argument array, and a definition that cannot be used is an <see cref="IOException"/>
/// (EC-MCS-IMP, §0 Q8). The process cases are <c>conformance-test:MessagingProcessTests</c>.
/// </summary>
public sealed class ServerDefinitionsTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("pb4745d-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private Func<string, ServerDefinition?> Read(string json)
    {
        File.WriteAllText(Path.Combine(_dir, ServerDefinitions.FileName), json);
        return ServerDefinitions.Read(_dir);
    }

    [Fact]
    public void NoFile_DefinesNothing()
    {
        Assert.Null(ServerDefinitions.Read(_dir)("SRV"));
    }

    [Fact]
    public void AFileWithoutServers_DefinesNothing_AndItsOtherMembersAreNotRead()
    {
        // The communication facility's members are its own (DESIGN-communication-facility.md §6): this reader ignores them, whatever they hold.
        Assert.Null(Read("""{ "queues": 7, "sources": "x" }""")("SRV"));
    }

    [Fact]
    public void ANameIsNormalized_SoTrailingSpacesAndCaseDoNotMatter()
    {
        var definitions = Read("""{ "servers": [ { "name": "Balance  ", "maxLength": 12, "start": ["prog", "a b", "c"] } ] }""");
        var found = definitions("BALANCE");
        Assert.NotNull(found);
        Assert.Equal(12, found.MaxLength);
        Assert.Equal(["prog", "a b", "c"], found.Start);
        Assert.True(found.HasStart);
        Assert.Null(definitions("OTHER"));
    }

    [Fact]
    public void AServerWithNeitherMember_HasNoLimitAndNoStart()
    {
        var found = Read("""{ "servers": [ { "name": "SRV" } ] }""")("SRV");
        Assert.NotNull(found);
        Assert.Null(found.MaxLength);
        Assert.False(found.HasStart);
    }

    [Theory]
    [InlineData("prog", new[] { "prog" })]
    [InlineData("prog a  b", new[] { "prog", "a", "b" })]
    [InlineData("\"C:\\Program Files\\x.exe\" --serve \"two words\"", new[] { "C:\\Program Files\\x.exe", "--serve", "two words" })]
    [InlineData("a\tb", new[] { "a", "b" })]
    [InlineData("a \"\" b", new[] { "a", "", "b" })]
    public void ACommandLineSplitsAtSpaces_AndQuotesGroup(string line, string[] expected)
    {
        Assert.Equal(expected, ServerDefinitions.SplitCommandLine(line));
    }

    [Fact]
    public void AStartGivenAsALine_IsSplit()
    {
        Assert.Equal(["dotnet", "x y.dll"], Read("""{ "servers": [ { "name": "S", "start": "dotnet \"x y.dll\"" } ] }""")("S")!.Start);
    }

    [Theory]
    [InlineData("""{ "servers": [ { "name": "S", "start": "\"open" } ] }""")]                     // an unclosed quote
    [InlineData("""{ "servers": [ { "name": "S", "start": [] } ] }""")]                          // names no program
    [InlineData("""{ "servers": [ { "name": "S", "start": "   " } ] }""")]
    [InlineData("""{ "servers": [ { "name": "S", "start": [ 1 ] } ] }""")]
    [InlineData("""{ "servers": [ { "name": "S", "start": 7 } ] }""")]
    [InlineData("""{ "servers": [ { "name": "S", "maxLength": 0 } ] }""")]
    [InlineData("""{ "servers": [ { "name": "S", "maxLength": -3 } ] }""")]
    [InlineData("""{ "servers": [ { "name": "S", "maxLength": 1.5 } ] }""")]
    [InlineData("""{ "servers": [ { "name": "S", "maxLength": "4" } ] }""")]
    [InlineData("""{ "servers": [ { "name": "  " } ] }""")]
    [InlineData("""{ "servers": [ { "name": 4 } ] }""")]
    [InlineData("""{ "servers": [ { } ] }""")]
    [InlineData("""{ "servers": [ 3 ] }""")]
    [InlineData("""{ "servers": { } }""")]
    [InlineData("""{ "servers": [ { "name": "S" }, { "name": "s " } ] }""")]                    // one server twice
    [InlineData("[ ]")]
    [InlineData("{ not json")]
    public void ADefinitionThatCannotBeUsed_IsAnIoException(string json)
    {
        Assert.Throws<IOException>(() => Read(json));
    }

    [Fact]
    public void AParsedFile_AnswersTheSameAsTheFile()
    {
        var definitions = ServerDefinitions.From((JsonObject)JsonNode.Parse("""{ "servers": [ { "name": "A", "maxLength": 3 } ] }""")!);
        Assert.Equal(3, definitions("A")!.MaxLength);
    }
}
