// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Tests.Shared;                             // TestRepo — the ONE repo-root locator
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ THE PB443 INVARIANT: a SEARCH operand is a WRITTEN REFERENCE, resolved by ISO §8.4.2.2 — never a BASE WORD
/// looked up and tie-broken by declaration order. Two lines did that (<c>SearchBinder.BindSearch</c> and
/// <c>BindSearchAll</c>, two copies of the same eight), and a third did it for the Format-2 condition-name
/// operand, so the compiler could pick a DIFFERENT table, and a different level-88, from the one the programmer
/// wrote.
/// <para>⛔ WHY THIS IS SEPARATE FROM <see cref="DecidedRuleStageDriftTests"/>. That class guards the STAGE of a
/// decided rule (an error naming the rule, never the COBOLNET1756 deferral announce), and it would pass in
/// exactly the state PB443 opened in: the wrong-answer half produced NO diagnostic at all, at any stage — it
/// compiled clean and returned the wrong value. What falsifies it is an ANSWER, so every row below runs the
/// program and reads what it printed.</para>
/// <para>⛔ AND WHY EVERY ROW COMES IN A PAIR WITH THE DECLARATION ORDER SWAPPED. A single qualified search that
/// happens to name the first-declared table passes under the defect. The pair is the isolation: swapping two 01
/// entries and nothing else must not change the answer, because §8.4.2.2 says the QUALIFIER establishes
/// uniqueness and §14.9.37.4 GR1 says the statement varies "the first or only index associated with
/// identifier-1" — the qualified one.</para>
/// </summary>
public sealed class SearchOperandResolutionDriftTests : CobolNetTestBase
{
    /// <summary>The COBOL source newline — free-form source is written LF-only regardless of host.</summary>
    private const string Nl = "\n";

    /// <summary>Two groups, each with a table <c>E</c> holding different data; only <c>G2</c>'s holds the WHEN's
    /// key. <paramref name="decoyFirst"/> chooses which 01 is declared first — the axis the defect was sensitive
    /// to and the standard is not.</summary>
    private static string TwoTables(string id, string statement, bool decoyFirst)
    {
        const string g1 = """
01 G1.
   05 E OCCURS 4 TIMES ASCENDING KEY IS K INDEXED BY IX1.
      10 K PIC 9(2).
""";
        const string g2 = """
01 G2.
   05 E OCCURS 4 TIMES ASCENDING KEY IS K INDEXED BY IX2.
      10 K PIC 9(2).
""";
        return $"""
IDENTIFICATION DIVISION.
PROGRAM-ID. {id}.
DATA DIVISION.
WORKING-STORAGE SECTION.
{(decoyFirst ? g1 + Nl + g2 : g2 + Nl + g1)}
PROCEDURE DIVISION.
MAIN.
    MOVE 91 TO K IN G1 (1). MOVE 92 TO K IN G1 (2).
    MOVE 93 TO K IN G1 (3). MOVE 94 TO K IN G1 (4).
    MOVE 11 TO K IN G2 (1). MOVE 12 TO K IN G2 (2).
    MOVE 13 TO K IN G2 (3). MOVE 14 TO K IN G2 (4).
    SET IX1 TO 1.
    SET IX2 TO 1.
{statement}
    STOP RUN.
""";
    }

    // Format 1 and Format 2, IN and OF — the four arms that print the same identifier-1 operand.
    [Theory]
    [InlineData("PB443R01", true, "SEARCH E IN G2")]
    [InlineData("PB443R02", false, "SEARCH E IN G2")]
    [InlineData("PB443R03", true, "SEARCH E OF G2")]
    [InlineData("PB443R04", false, "SEARCH E OF G2")]
    [InlineData("PB443R05", true, "SEARCH ALL E IN G2")]
    [InlineData("PB443R06", false, "SEARCH ALL E IN G2")]
    [InlineData("PB443R07", true, "SEARCH ALL E OF G2")]
    [InlineData("PB443R08", false, "SEARCH ALL E OF G2")]
    public void QualifiedIdentifier1_SelectsByTheQualifier_NotByDeclarationOrder(
        string id, bool decoyFirst, string head)
    {
        string stmt = $"""
    {head}
        AT END DISPLAY "NONE"
        WHEN K IN G2 (IX2) = 13 DISPLAY "HIT " K IN G2 (IX2)
    END-SEARCH.
""";
        var (ok, detail, stdout) = Run(TwoTables(id, stmt, decoyFirst));
        Assert.True(ok, detail);
        // §14.9.37.4 GR1a — the index is left at the occurrence whose WHEN was satisfied, occurrence 3 of G2's E.
        Assert.Equal("HIT 13", stdout.Trim());
    }

    /// <summary>§8.4.2.2 Format 2 for the Format-2 WHEN's condition-name operand, in both SR11 directions —
    /// the arm where the compiler REJECTED legal source. With <c>ASCENDING KEY IS K2 K1</c>, K2 is the most
    /// significant key (§13.18.38.4 GR3), so a WHEN naming only K2's condition-name satisfies SR11 ("all
    /// preceding data-names … shall also be referenced" — there are none); with <c>K1 K2</c> the same written
    /// reference skips K1 and violates it. The screen used to pick the level-88 by base word with the tie broken
    /// by declaration order, so it got BOTH verdicts backwards.</summary>
    [Theory]
    [InlineData("PB443C01", "K2 K1", true)]
    [InlineData("PB443C02", "K1 K2", false)]
    public void QualifiedConditionName_InAFormat2When_SelectsByTheQualifier(
        string id, string keyPhrase, bool legal)
    {
        string src = $"""
IDENTIFICATION DIVISION.
PROGRAM-ID. {id}.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 T.
   05 E OCCURS 4 TIMES ASCENDING KEY IS {keyPhrase} INDEXED BY IX.
      10 K1 PIC 9(2).
         88 CN VALUE 03.
      10 K2 PIC 9(2).
         88 CN VALUE 07.
PROCEDURE DIVISION.
MAIN.
    MOVE 09 TO K1 (1). MOVE 05 TO K2 (1).
    MOVE 08 TO K1 (2). MOVE 06 TO K2 (2).
    MOVE 07 TO K1 (3). MOVE 07 TO K2 (3).
    MOVE 06 TO K1 (4). MOVE 08 TO K2 (4).
    SEARCH ALL E
        AT END DISPLAY "NONE"
        WHEN CN OF K2 (IX) DISPLAY "HIT " K2 (IX)
    END-SEARCH.
    STOP RUN.
""";
        var (ok, detail, stdout) = Run(src);
        if (legal)
        {
            Assert.True(ok, detail);
            Assert.Equal("HIT 07", stdout.Trim());
        }
        else
        {
            Assert.False(ok, stdout);
            Assert.Contains("COBOLNET1965", detail, StringComparison.Ordinal);
            Assert.Contains("§14.9.37.3 SR11", detail, StringComparison.Ordinal);
        }
    }

    /// <summary>§14.9.37.4 GR1 — "The subscript that is used to determine the occurrence of each superordinate
    /// table to search is specified by the user in the WHEN phrases." The superordinate subscript SR3 requires on
    /// identifier-1 is therefore not decoration: the search runs inside the outer occurrence the WHEN names, and
    /// only identifier-1's own index is varied. With OX set to 2, the match is the SECOND row's.</summary>
    [Fact]
    public void NestedTable_SearchesTheSuperordinateOccurrenceTheWhenNames()
    {
        var (ok, detail, stdout) = Run("""
IDENTIFICATION DIVISION.
PROGRAM-ID. PB443N01.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 WS-OX PIC 9(1).
01 NEST.
   05 OUTER OCCURS 2 TIMES INDEXED BY OX.
      10 INNER OCCURS 4 TIMES INDEXED BY IX3.
         15 NK PIC 9(2).
PROCEDURE DIVISION.
MAIN.
    MOVE 11 TO NK (1 1). MOVE 12 TO NK (1 2).
    MOVE 13 TO NK (1 3). MOVE 14 TO NK (1 4).
    MOVE 21 TO NK (2 1). MOVE 22 TO NK (2 2).
    MOVE 23 TO NK (2 3). MOVE 24 TO NK (2 4).
    SET OX TO 2.
    SET IX3 TO 1.
    SEARCH INNER (OX)
        AT END DISPLAY "NONE"
        WHEN NK (OX IX3) = 23
            SET WS-OX TO OX
            DISPLAY "HIT " NK (OX IX3) " OX=" WS-OX
    END-SEARCH.
    STOP RUN.
""");
        Assert.True(ok, detail);
        // OX is UNCHANGED by the search (GR1 — "only the setting of an index associated with identifier-1 …
        // is modified"), so it still names the outer occurrence the program set and the WHEN used.
        Assert.Equal("HIT 23 OX=2", stdout.Trim());
    }

    /// <summary>⛔ THE STRUCTURAL GUARD, so "one decomposition" stays true: <c>SearchBinder</c> holds no
    /// <c>cobolWord()</c> reduction of an operand at all. Every reference it binds goes through
    /// <c>ReferenceResolver</c>, which reads the whole written reference (<c>ReadWritten</c>) before it looks a
    /// name up. A future operand added to SEARCH with the old idiom fails HERE rather than in a user's program.
    /// </summary>
    [Fact]
    public void SearchBinder_ReducesNoOperandToItsBaseWord()
    {
        string path = TestRepo.Src("Cobol.Net.Compiler", "Binding", "Procedure", "Verbs", "SearchBinder.cs");
        Assert.True(File.Exists(path), path);
        // CODE lines only — the doc comments SAY "cobolWord()" to record what was removed, and a guard that
        // could be satisfied by deleting a comment would be measuring the wrong thing.
        string code = string.Join(Nl, File.ReadAllLines(path)
            .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
        Assert.DoesNotContain("cobolWord()", code, StringComparison.Ordinal);
        Assert.DoesNotContain("TryResolve(", code, StringComparison.Ordinal);
    }

    /// <summary>Compile and RUN at the default edition, returning what the program printed — the ANSWER is the
    /// thing under test, so a row that only compiled would pass in the state this note opened in.</summary>
    private (bool Ok, string Detail, string Stdout) Run(string source)
    {
        var (ok, stdout, detail) = CompileAndRun(source, dialectLevel: 2023);
        return (ok, detail, stdout);
    }
}
