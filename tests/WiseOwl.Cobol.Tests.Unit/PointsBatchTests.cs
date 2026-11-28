// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.Json;
using WiseOwl.Cobol.Compiler;
using WiseOwl.Cobol.Cli;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// <c>cobol points-batch</c> and <see cref="ParseNodeReader"/> (kb/Work PB6050): the parse of a program read as token streams for the
/// conformance suite's coverage points (DESIGN-conformance-suite.md section 3.4). The reader decides nothing about a program, so
/// these hold its three promises: a frame's node is its tokens in order, a nested statement is one token, and an operand
/// context of one token is that token while a longer one is one placeholder.
/// </summary>
public sealed class PointsBatchTests
{
    private const string Program85 =
        "       IDENTIFICATION DIVISION.\n"
        + "       PROGRAM-ID. PB6050.\n"
        + "       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n"
        + "       01 A PIC 9.\n"
        + "       01 T.\n"
        + "           02 B PIC 9 OCCURS 3.\n"
        + "       PROCEDURE DIVISION.\n"
        + "           MOVE 1 TO A.\n"
        + "           IF A = 1 DISPLAY \"X\" ELSE MOVE 2 TO B (A).\n"
        + "           STOP RUN.\n";

    private static HashSet<string> Set(params string[] rules) => [.. rules];

    private static ParseNodeReader.Reading Read(string text, ParseNodeReader.Selection selection, int std = 85)
    {
        string dir = ScratchDirectory.Create("pb6050points");
        try
        {
            string src = Path.Combine(dir, "P.cob");
            File.WriteAllText(src, text);
            return ParseNodeReader.Read(new CompilerDriver.Options(src, EditionInfo.Of(std)), selection);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static string Spell(ParseNodeReader.Node node) =>
        string.Join(' ', node.Tokens.Select(t => t.Type == ParseNodeReader.PlaceholderType ? $"<{t.Text.Trim('<', '>')}>" : t.Text));

    /// <summary>A frame's node is its tokens in order; an operand context of one token is that token, whose lexer type says what it is, and
    /// a context of several tokens is one placeholder named by its rule.</summary>
    [Fact]
    public void ANode_IsItsTokensInOrder_AndAnOperandIsOneItem()
    {
        var reading = Read(Program85, new(Set("moveStatement"), Set("dataReference", "literal"), Set()));
        Assert.True(reading.Parsed);
        Assert.Equal(["MOVE 1 TO A", "MOVE 2 TO <dataReference>"], reading.Nodes.Select(Spell).ToArray());
        Assert.Equal(["IDENTIFIER"], reading.Nodes[0].Tokens.Where(t => t.Text == "A").Select(t => t.Type).ToArray());
        Assert.Equal("INTEGERLIT", reading.Nodes[0].Tokens[1].Type);
    }

    /// <summary>A statement nested in a node is one placeholder token, whatever it holds: the IF's text shows its two branches as
    /// two statements, and each branch is read as a node of its own.</summary>
    [Fact]
    public void ANestedStatement_IsOneToken()
    {
        var reading = Read(Program85, new(Set("ifStatement", "displayStatement"), Set("condition", "literal"), Set()));
        Assert.Equal(["IF <condition> <statement> ELSE <statement>", "DISPLAY \"X\""], reading.Nodes.Select(Spell).ToArray());
    }

    /// <summary>A frame named flat keeps every token of its operands: a subscripted reference shows its subscript.</summary>
    [Fact]
    public void AFlatFrame_KeepsItsOperandsTokens()
    {
        var reading = Read(Program85, new(Set("dataReference"), Set("dataReference", "arithmeticExpression"), Set("dataReference")));
        Assert.Contains("B ( A )", reading.Nodes.Select(Spell));
    }

    /// <summary>A program the front end refuses has no parse to read: the entry is <c>parsed: false</c> with no nodes, never a guess.</summary>
    [Fact]
    public void AProgramWithASyntaxError_HasNoNodes()
    {
        var reading = Read("       IDENTIFICATION DIVISION.\n       PROGRAM-ID. P.\n       PROCEDURE DIVISION.\n           MOVE TO.\n",
            new(Set("moveStatement"), Set(), Set()));
        Assert.False(reading.Parsed);
        Assert.Empty(reading.Nodes);
    }

    /// <summary>The manifest line is <c>source TAB std TAB source-format</c>; a std or a format that is no value the compiler accepts
    /// is <c>parsed: false</c>, never read at a default (the sibling of kb/Work PB4126's check-batch rule).</summary>
    [Theory]
    [InlineData("85", "fixed", true)]
    [InlineData("1999", "fixed", false)]
    [InlineData("85", "", false)]
    [InlineData("85", "dense", false)]
    public void ABatchLine_ParsesUnderItsEditionAndFormat_OrNotAtAll(string std, string format, bool parsed)
    {
        string dir = ScratchDirectory.Create("pb6050line");
        try
        {
            string src = Path.Combine(dir, "P.cob");
            File.WriteAllText(src, Program85);
            using var json = JsonDocument.Parse(Program.PointsBatchLine($"{src}\t{std}\t{format}", new(Set("moveStatement"), Set(), Set())));
            Assert.Equal(parsed, json.RootElement.GetProperty("parsed").GetBoolean());
            Assert.Equal(parsed ? 2 : 0, json.RootElement.GetProperty("nodes").GetArrayLength());
            Assert.Equal(std, json.RootElement.GetProperty("std").GetString());
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>points-batch is its own verb, like check-batch: it takes a manifest and its rule lists, and a source before it is a
    /// usage error.</summary>
    [Fact]
    public void PointsBatch_IsItsOwnVerb()
    {
        Assert.Empty(Program.Parse([Program.PointsBatchName, "m.tsv", "--frames", "a", "--operands", "b"]).Errors);
        Assert.NotEmpty(Program.Parse([Program.PointsBatchName, "m.tsv", "--operands", "b"]).Errors);   // --frames is required
        Assert.NotEmpty(Program.Parse(["a.cob", Program.PointsBatchName, "m.tsv", "--frames", "a", "--operands", "b"]).Errors);
    }
}
