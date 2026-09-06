// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §12.4.5.7 — the file control entry's COLLATING SEQUENCE clause, per edition.
/// <para>The subject is §12.4.5.7.3 SR8 — <i>"Neither data-name-1 nor record-key-name-1 shall be specified in
/// more than one COLLATING SEQUENCE clause"</i> — and specifically the BOUNDARY it draws. §12.4.5.7.2's Format-2
/// figure is <c>COLLATING SEQUENCE OF { data-name-1 | record-key-name-1 } … IS alphabet-name-3</c>, with the
/// ellipsis printed immediately right of the closing brace; by §5.2.7 the repeated portion is therefore the
/// BRACE GROUP, and no rule adds a distinctness requirement to the repetition. So one clause may list the same
/// key twice, and one clause is never "more than one" — the rule counts CLAUSES, not occurrences.</para>
/// <para>Both arms are pinned here because the defect this class was opened for (kb/Work PB703) satisfied the
/// rejecting arm perfectly: the screen's <c>HashSet</c> sat OUTSIDE the loop over the clauses while the
/// <c>Add</c> ran inside the per-name loop, so it screened per NAME across all clauses at once and rejected
/// <c>COLLATING SEQUENCE OF IX-KEY IX-KEY IS REV</c> with a diagnostic the program itself falsified. A
/// rejection-only suite cannot see that; the accepting arms are the drift guard.</para>
/// </summary>
public sealed class FileCollatingSequenceSpecTests
{
    /// <summary>The editions in which the key-level (Format 2) COLLATING SEQUENCE clause exists — a COBOL-2002
    /// addition of the expanded indexed I-O module. The 85 lane is the edition-GATE test below, not a behaviour
    /// lane.</summary>
    public static TheoryData<int> KeyLevelEditions() => new() { 2002, 2014, 2023 };

    private const string Sr8Text = "more than one COLLATING SEQUENCE clause";

    /// <summary>A minimal INDEXED file control entry carrying <paramref name="collatingClauses"/> as the
    /// COLLATING SEQUENCE text of its file control entry (one clause per element; the entry's period is
    /// appended to the last). Two alphabets are declared so a two-clause case can name two different ones, and
    /// the program body is inert because every rule under test is a SYNTAX rule, settled at bind time.</summary>
    private static string Program(string programId, params string[] collatingClauses) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {programId}.
        ENVIRONMENT DIVISION.
        CONFIGURATION SECTION.
        SPECIAL-NAMES.
            ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
            ALPHABET DREV IS "9876543210".
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT IXF ASSIGN TO "fcs-collating.dat"
                ORGANIZATION IS INDEXED
                ACCESS MODE IS DYNAMIC
                RECORD KEY IS IX-KEY
                ALTERNATE RECORD KEY IS IX-ALT
        {Clauses(collatingClauses)}
        DATA DIVISION.
        FILE SECTION.
        FD IXF.
        01 IX-REC.
           05 IX-KEY  PIC X(1).
           05 IX-ALT  PIC X(1).
           05 IX-DATA PIC X(8).
        WORKING-STORAGE SECTION.
        01 WS-N PIC 9 VALUE 0.
        PROCEDURE DIVISION.
        MAIN.
            MOVE 1 TO WS-N.
            STOP RUN.
        """;

    /// <summary>One source line per clause (the interpolation hole sits at column 0 of its own line, so each
    /// produced line carries its own indentation) with the file control entry's terminating period on the
    /// last.</summary>
    private static string Clauses(string[] clauses) => string.Join("\n",
        clauses.Select((c, i) => "        " + c + (i == clauses.Length - 1 ? "." : "")));

    [Theory]   // SR8's boundary is the CLAUSE: one clause naming IX-KEY twice is legal source (kb/Work PB703).
    [MemberData(nameof(KeyLevelEditions))]
    public void KeyLevel_SameKeyTwiceInOneClause_IsAccepted(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile(
            Program("FCS001", "COLLATING SEQUENCE OF IX-KEY IX-KEY IS REV"), edition);
        Assert.True(ok, $"[--std {edition}] one clause is not more than one clause: "
            + string.Join("\n", diagnostics));
    }

    [Theory]   // SR8 itself: the SAME key named by TWO clauses of one file control entry is the violation.
    [MemberData(nameof(KeyLevelEditions))]
    public void KeyLevel_SameKeyInTwoClauses_Diagnosed(int edition)
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Program("FCS002",
            "COLLATING SEQUENCE OF IX-KEY IS REV",
            "COLLATING SEQUENCE OF IX-ALT IX-KEY IS DREV"), edition), Sr8Text);

    [Theory]   // ONE violation is ONE diagnostic. The register remembers the LAST clause that named a key, not
    [MemberData(nameof(KeyLevelEditions))]   // the first, so the second clause's SECOND write of IX-KEY compares
    // against the clause it is IN and stays silent; a first-seen ordinal reported this program TWICE (measured
    // on 8ef11ec8, kb/Work PB703 — the same regression PB364's USE screens showed).
    public void KeyLevel_SameKeyRepeatedInTheSecondClause_DiagnosedOnce(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program("FCS003",
            "COLLATING SEQUENCE OF IX-KEY IS REV",
            "COLLATING SEQUENCE OF IX-KEY IX-KEY IS DREV"), edition);
        Assert.False(ok, $"[--std {edition}] two clauses naming IX-KEY violate SR8");
        Assert.Equal(1, diagnostics.Count(d => d.Contains(Sr8Text, StringComparison.Ordinal)));
    }

    [Theory]   // The complement of the register's scope: DIFFERENT keys in different clauses is the very thing
    [MemberData(nameof(KeyLevelEditions))]   // §12.4.5.7.1 exists for — "Multiple collating sequences may be used
    // by specifying a collating sequence clause unique to the primary key or to specific alternate record keys."
    public void KeyLevel_DifferentKeyPerClause_IsAccepted(int edition)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program("FCS004",
            "COLLATING SEQUENCE OF IX-KEY IS REV",
            "COLLATING SEQUENCE OF IX-ALT IS DREV"), edition);
        Assert.True(ok, $"[--std {edition}] one clause per key is what §12.4.5.7.1 describes: "
            + string.Join("\n", diagnostics));
    }

    [Theory]   // §12.4.5.7.3 SR3: "Only one file-level format COLLATING SEQUENCE clause may be specified in one
    [MemberData(nameof(KeyLevelEditions))]   // file control entry." The file-level format is Format 1 (the IS /
                                             // FOR forms), counted separately from the key-level clauses above.
    public void FileLevel_TwoClausesInOneFileControlEntry_Diagnosed(int edition)
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Program("FCS006",
            "COLLATING SEQUENCE IS REV",
            "COLLATING SEQUENCE IS DREV"), edition), "at most one file-level");

    [Theory]   // §12.4.5.7.3 SR4: "Data-name-1 shall be a name specified as the data-name in an ALTERNATE RECORD
    [MemberData(nameof(KeyLevelEditions))]   // KEY clause or in a RECORD KEY clause in the file control entry."
                                             // IX-DATA is a record item of IXF but no key of it, so the clause
                                             // has nothing to apply alphabet-name-3 to.
    public void KeyLevel_NameThatIsNotAKey_Diagnosed(int edition)
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(
            Program("FCS007", "COLLATING SEQUENCE OF IX-DATA IS REV"), edition),
            "is not a RECORD KEY or ALTERNATE RECORD KEY");

    [Fact]   // The edition gate (feedback_edition_gate_sweep): the file COLLATING SEQUENCE clause is a COBOL-2002
             // addition, so the SAME source the theories above accept is rejected at 85 — and rejected by the
             // GATE, never by SR8, which the one-clause repeat does not violate at any edition.
    public void KeyLevel_At85_IsEditionGated()
    {
        var diagnostics = EditionHarness.GetDiagnostics(
            Program("FCS005", "COLLATING SEQUENCE OF IX-KEY IX-KEY IS REV"), 85);
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOLNET0900");
        EditionHarness.AssertNoDiagnostic(diagnostics, Sr8Text);
    }
}
