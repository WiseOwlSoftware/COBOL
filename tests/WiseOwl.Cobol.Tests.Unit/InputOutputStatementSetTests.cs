// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Antlr4.Runtime.Tree;
using WiseOwl.Cobol.Binding;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Diagnostics;
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;
using CnFrontend = WiseOwl.Cobol.Frontend.Frontend;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ EACH EDITION'S SET OF INPUT-OUTPUT STATEMENTS IS ITS OWN TEXT'S (kb/Work PB6032). <see cref="InputOutputStatementSet"/> holds one
/// value per edition and <see cref="InputOutputStatementForms"/> is the one classification of a statement against it. Two guards:
/// <list type="number">
/// <item>The DRIFT guard re-reads each edition's sentence from its transcription on every run and requires the edition's value to be
/// exactly the forms it prints: COBOL-85's definition (§1985:III.2 p139, "The input-output statements are: ..."), and the
/// statements whose execution sets the I-O status in the later editions (§2002:9.1.12, §2014:9.1.12, §2023:9.1.13.1). A form
/// added to or dropped from one value without its edition's text turns this red.</item>
/// <item>The CLASSIFICATION guard parses a COBOL-85 program that writes every form, and the statements that look like one and are
/// not (ACCEPT MESSAGE COUNT, a SET of an index, a MOVE), and asks each statement's form and its membership at every edition.</item>
/// </list>
/// </summary>
[Trait("Sweep", "tree")]
public sealed class InputOutputStatementSetTests
{
    // ── The drift guard ──────────────────────────────────────────────────────────────────────────────────────

    /// <summary>The form a printed list item names: the two the 1985 definition qualifies by a phrase, else the verb.</summary>
    private static IoStatementForms Printed(string item) => item.Trim() switch
    {
        "ACCEPT (with the identifier phrase)" => IoStatementForms.AcceptIdentifier,
        "SET (with the TO ON or TO OFF phrase)" => IoStatementForms.SetSwitch,
        var verb when Regex.IsMatch(verb, "^[A-Z]+$") => Enum.Parse<IoStatementForms>(verb, ignoreCase: true),
        var other => throw new InvalidOperationException($"the printed list item '{other}' names no form"),
    };

    private static IoStatementForms Union(IEnumerable<string> items) =>
        items.Aggregate(IoStatementForms.None, (set, item) => set | Printed(item));

    /// <summary>The 1985 definition's list, read from the glossary entry: the text after "The input-output statements are:" up to its
    /// closing period, items separated by commas and a final "and".</summary>
    private static IoStatementForms Read1985()
    {
        string entry = File.ReadAllLines(TestRepo.Specs("ANSI_COBOL_1985.md"))
            .Single(l => l.StartsWith("<u>Input-Output Statement</u>.", StringComparison.Ordinal));
        var m = Regex.Match(entry, @"The input-output statements are: (?<list>.+)\.$");
        Assert.True(m.Success, $"the 1985 definition no longer prints its list: {entry}");
        return Union(Regex.Split(m.Groups["list"].Value, @",\s*(?:and\s+)?"));
    }

    /// <summary>A later edition's list, read from <paramref name="spec"/> (its transcription's path, which each caller spells,
    /// so the build-targets audit resolves every read): the statements the first sentence of its I-O status clause (the heading
    /// <paramref name="heading"/>) says set the I-O status, "during the execution of a X, Y ... or Z statement".</summary>
    private static IoStatementForms ReadIoStatus(string spec, string heading)
    {
        var lines = File.ReadAllLines(spec);
        int at = Array.FindIndex(lines, l => l.Trim() == heading);
        Assert.True(at >= 0, $"{spec} has no heading '{heading}'");
        string sentence = lines.Skip(at + 1).First(l => l.Contains("I-O status", StringComparison.Ordinal));
        var m = Regex.Match(sentence, @"during the execution of a (?<list>[A-Z, ]+ or [A-Z]+) statement");
        Assert.True(m.Success, $"{spec} '{heading}' no longer names the statements that set the I-O status: {sentence}");
        return Union(Regex.Split(m.Groups["list"].Value, @",\s*|\s+or\s+"));
    }

    [Fact]
    public void TheReader_IsAbleToFail_ItReadsFifteenAndEight()
    {
        Assert.Equal(15, System.Numerics.BitOperations.PopCount((uint)Read1985()));
        Assert.Equal(8, System.Numerics.BitOperations.PopCount((uint)ReadIoStatus(TestRepo.Specs("ISO_COBOL_2023.md"), "##### 9.1.13.1 General")));
        Assert.Throws<InvalidOperationException>(() => Printed("MOVE (with the CORRESPONDING phrase)"));
    }

    [Fact]
    public void Cobol85_IsTheFifteenOfTheDefinition() => Assert.Equal(Read1985(), InputOutputStatementSet.Of(85));

    [Fact]
    public void Cobol2002_IsTheEightThatSetTheIoStatus() =>
        Assert.Equal(ReadIoStatus(TestRepo.Specs("ISO_COBOL_2002.md"), "##### 9.1.12 I-O status"), InputOutputStatementSet.Of(2002));

    [Fact]
    public void Cobol2014_IsTheEightThatSetTheIoStatus() =>
        Assert.Equal(ReadIoStatus(TestRepo.Specs("ISO_COBOL_2014.md"), "##### 9.1.12 I-O status"), InputOutputStatementSet.Of(2014));

    [Fact]
    public void Cobol2023_IsTheEightThatSetTheIoStatus() =>
        Assert.Equal(ReadIoStatus(TestRepo.Specs("ISO_COBOL_2023.md"), "##### 9.1.13.1 General"), InputOutputStatementSet.Of(2023));

    [Fact]
    public void EveryEdition_HasAValue()
    {
        foreach (int year in EditionInfo.All) Assert.NotEqual(IoStatementForms.None, InputOutputStatementSet.Of(year));
        Assert.Throws<ArgumentOutOfRangeException>(() => InputOutputStatementSet.Of(1999));
    }

    // ── The classification guard ─────────────────────────────────────────────────────────────────────────────

    private const string Program85 = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. IOSET85.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               SPECIAL-NAMES.
                   SWITCH-1 IS SW1 ON STATUS IS SW1-ON.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F1 ASSIGN TO "F1"
                       ORGANIZATION IS RELATIVE
                       ACCESS MODE IS DYNAMIC
                       RELATIVE KEY IS RK.
               DATA DIVISION.
               FILE SECTION.
               FD  F1.
               01  F1-REC PIC X(10).
               WORKING-STORAGE SECTION.
               01  RK PIC 9(4).
               01  X PIC X(10).
               01  T.
                   02 E PIC X OCCURS 3 INDEXED BY IX.
               COMMUNICATION SECTION.
               CD  CDI FOR INPUT.
               01  CDI-REC PIC X(87).
               CD  CDO FOR OUTPUT.
               01  CDO-REC PIC X(23).
               PROCEDURE DIVISION.
               P1.
                   ACCEPT X.
                   ACCEPT X FROM DATE.
                   ACCEPT CDI MESSAGE COUNT.
                   OPEN I-O F1.
                   READ F1 NEXT RECORD AT END CONTINUE.
                   WRITE F1-REC INVALID KEY CONTINUE.
                   REWRITE F1-REC INVALID KEY CONTINUE.
                   START F1 KEY IS EQUAL TO RK INVALID KEY CONTINUE.
                   DELETE F1 RECORD INVALID KEY CONTINUE.
                   CLOSE F1.
                   DISPLAY X.
                   SET SW1 TO ON.
                   SET IX TO 1.
                   ENABLE INPUT CDI KEY X.
                   DISABLE INPUT CDI KEY X.
                   RECEIVE CDI MESSAGE INTO X NO DATA CONTINUE.
                   SEND CDO FROM X WITH EMI.
                   PURGE CDO.
                   MOVE X TO F1-REC.
                   STOP RUN.
        """;

    private static List<CobolParserCore.StatementContext> Statements()
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_ioset_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, Program85.Replace("\r\n", "\n"));
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { InitialFormat = InitialReferenceFormat.Fixed, DialectLevel = 85 }.Parse(path, diags);
            Assert.NotNull(tree);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics.Select(d => d.ToString())));
            var found = new List<CobolParserCore.StatementContext>();
            Collect(tree!, found);
            return found;
        }
        finally { FileHarness.TryDelete(path); }

        static void Collect(IParseTree node, List<CobolParserCore.StatementContext> into)
        {
            if (node is CobolParserCore.StatementContext s) into.Add(s);
            for (int i = 0; i < node.ChildCount; i++) Collect(node.GetChild(i), into);
        }
    }

    /// <summary>Each statement of <see cref="Program85"/> in order (a nested CONTINUE included), with the form it is written in.</summary>
    private static readonly (string Verb, IoStatementForms Form)[] Expected =
    [
        ("ACCEPT", IoStatementForms.AcceptIdentifier), ("ACCEPT", IoStatementForms.AcceptIdentifier), ("ACCEPT", IoStatementForms.None),
        ("OPEN", IoStatementForms.Open), ("READ", IoStatementForms.Read), ("CONTINUE", IoStatementForms.None),
        ("WRITE", IoStatementForms.Write), ("CONTINUE", IoStatementForms.None), ("REWRITE", IoStatementForms.Rewrite),
        ("CONTINUE", IoStatementForms.None), ("START", IoStatementForms.Start), ("CONTINUE", IoStatementForms.None),
        ("DELETE", IoStatementForms.Delete), ("CONTINUE", IoStatementForms.None), ("CLOSE", IoStatementForms.Close),
        ("DISPLAY", IoStatementForms.Display), ("SET", IoStatementForms.SetSwitch), ("SET", IoStatementForms.None),
        ("ENABLE", IoStatementForms.Enable), ("DISABLE", IoStatementForms.Disable), ("RECEIVE", IoStatementForms.Receive),
        ("CONTINUE", IoStatementForms.None), ("SEND", IoStatementForms.Send), ("PURGE", IoStatementForms.Purge),
        ("MOVE", IoStatementForms.None), ("STOP", IoStatementForms.None),
    ];

    [Fact]
    public void EveryStatement_IsSortedIntoTheFormItIsWrittenIn()
    {
        var statements = Statements();
        Assert.Equal(Expected.Select(e => e.Verb), statements.Select(s => s.Start.Text.ToUpperInvariant()));
        Assert.Equal(Expected.Select(e => e.Form), statements.Select(InputOutputStatementForms.FormOf));
        // Every form of the 1985 set is written in the program, so the classification of each is exercised.
        Assert.Equal(InputOutputStatementSet.Cobol85,
            Expected.Aggregate(IoStatementForms.None, (set, e) => set | e.Form));
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AStatementIsAnInputOutputStatement_ExactlyWhenItsFormIsInTheEditionsSet(int year)
    {
        var edition = EditionInfo.Of(year);
        var set = InputOutputStatementSet.Of(year);
        foreach (var s in Statements())
        {
            var form = InputOutputStatementForms.FormOf(s);
            bool expected = form != IoStatementForms.None && set.HasFlag(form);
            var node = InputOutputStatementForms.Of(s, edition);
            Assert.Equal(expected, node is not null);
            if (node is not null) Assert.Same(s.GetChild(0), node);
        }
    }

    /// <summary>The difference that makes the 1985 set its own: DISPLAY, ACCEPT identifier, the switch SET and the communication
    /// statements are input-output statements at 85 and at no later edition.</summary>
    [Fact]
    public void Cobol85_AndCobol2023_DifferOnTheNucleusAndCommunicationForms()
    {
        var only85 = InputOutputStatementSet.Of(85) & ~InputOutputStatementSet.Of(2023);
        Assert.Equal(IoStatementForms.AcceptIdentifier | IoStatementForms.Disable | IoStatementForms.Display | IoStatementForms.Enable
            | IoStatementForms.Purge | IoStatementForms.Receive | IoStatementForms.Send | IoStatementForms.SetSwitch, only85);
        Assert.Equal(IoStatementForms.Unlock, InputOutputStatementSet.Of(2023) & ~InputOutputStatementSet.Of(85));
    }
}
