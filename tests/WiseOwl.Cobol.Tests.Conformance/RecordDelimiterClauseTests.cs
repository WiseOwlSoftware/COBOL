// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The RECORD DELIMITER clause's operand and placement, at --std 85 and --std 2023 (kb/Work PB5844). The operand is
/// STANDARD-1 (declined, COBOL1778) or an implementor-name: §1985:VII.2.8.4 GR3 makes the method "that associated with
/// implementor-name-1 by the implementor" and §2023:12.4.5.11.3 SR2 says "The implementor shall specify the names
/// available for use as feature-name-1". The names are BINARY-SEQUENTIAL and LINE-SEQUENTIAL (docs/CONFORMANCE.md
/// DOC-A.1-150); any other word is refused (COBOL7598). The clause "may be specified only for variable-length records"
/// (§2023:12.4.5.11.3 SR1, §1985:VII.2.8.3 SR1), on either arm (COBOL7599).
/// <para>Each refusal is paired with the nearest program that compiles, so a screen that refused every RECORD
/// DELIMITER clause, or none, fails a fact here: the output golden of the methods is
/// <c>wiseowl/cases/12.4.5.11-RECORD-DELIMITER/pb5844_rd_implementor_names</c>.</para>
/// </summary>
public sealed class RecordDelimiterClauseTests
{
    private const string NameUndefined = "COBOL7598";
    private const string OnFixedLength = "COBOL7599";

    /// <summary>A program with one file: <paramref name="selectTail"/> follows <c>SELECT F1 ASSIGN TO "rdt.dat"</c>,
    /// <paramref name="fd"/> follows <c>FD F1</c>, and <paramref name="body"/> is the procedure division.</summary>
    private static string Program(string selectTail, string fd, string records = "       01 R1 PIC X(10).\n",
        string body = "           OPEN OUTPUT F1\n           CLOSE F1\n") =>
        "       IDENTIFICATION DIVISION.\n"
        + "       PROGRAM-ID. RDT.\n"
        + "       ENVIRONMENT DIVISION.\n"
        + "       INPUT-OUTPUT SECTION.\n"
        + "       FILE-CONTROL.\n"
        + "           SELECT F1 ASSIGN TO \"rdt.dat\"\n"
        + selectTail + ".\n"
        + "       DATA DIVISION.\n"
        + "       FILE SECTION.\n"
        + "       FD F1" + fd + ".\n"
        + records
        + "       WORKING-STORAGE SECTION.\n"
        + "       01 WS-LEN PIC 99.\n"
        + "       PROCEDURE DIVISION.\n"
        + "       MAIN-PARA.\n"
        + body
        + "           STOP RUN.\n";

    private const string Varying = "\n           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS";

    private static void AssertRefused(string source, int edition, string code, params string[] named)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.False(ok, $"--std {edition}: expected {code}, the program compiled");
        var hits = errors.Where(e => e.Contains(code, StringComparison.Ordinal)).ToList();
        Assert.True(hits.Count == 1, $"--std {edition}: expected exactly one {code}; got:\n{string.Join("\n", errors)}");
        foreach (string text in named) Assert.Contains(text, hits[0], StringComparison.Ordinal);
    }

    private static void AssertCompiles(string source, int edition)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(source, edition);
        Assert.True(ok, $"--std {edition}: {string.Join("\n", errors)}");
    }

    /// <summary>A word that is no implementor-name is refused, and the refusal names it and the names that exist: an
    /// arbitrary word, and the file's own name (the checker's rdfoo and rdown programs).</summary>
    [Theory]
    [InlineData(85, "FOOBAR")]
    [InlineData(2023, "FOOBAR")]
    [InlineData(85, "F1")]
    [InlineData(2023, "F1")]
    public void UndefinedWord_IsRefused(int edition, string word)
    {
        string source = Program($"               RECORD DELIMITER IS {word}", Varying);
        AssertRefused(source, edition, NameUndefined, $"RECORD DELIMITER IS {word} names no method",
            "BINARY-SEQUENTIAL and LINE-SEQUENTIAL");
    }

    /// <summary>Each implementor-name, with and without IS and in any letter case, compiles clean on a variable-length
    /// file: no COBOL1778, no COBOL7598.</summary>
    [Theory]
    [InlineData(85, "RECORD DELIMITER IS BINARY-SEQUENTIAL")]
    [InlineData(2023, "RECORD DELIMITER binary-sequential")]
    [InlineData(85, "RECORD DELIMITER LINE-SEQUENTIAL")]
    [InlineData(2023, "RECORD DELIMITER IS Line-Sequential")]
    public void ImplementorName_CompilesWithNoDiagnostic(int edition, string clause)
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Program("               " + clause, Varying), edition);
        Assert.True(ok, $"--std {edition}: {string.Join("\n", errors)}");
        Assert.DoesNotContain(warnings, w => w.Contains("RECORD DELIMITER", StringComparison.Ordinal));
    }

    /// <summary>SR1 on both arms: a format 1 RECORD clause, and an entry with no RECORD clause whose one record is
    /// fixed-length (Annex A.1 item 147), are fixed-length records, so the clause is refused once.</summary>
    [Theory]
    [InlineData(85, "BINARY-SEQUENTIAL", "\n           RECORD CONTAINS 10 CHARACTERS", "§1985:VII.2.8.3 SR1")]
    [InlineData(2023, "BINARY-SEQUENTIAL", "\n           RECORD CONTAINS 10 CHARACTERS", "§2023:12.4.5.11.3 SR1")]
    [InlineData(85, "STANDARD-1", "", "§1985:II.2.1.4.3")]
    [InlineData(2023, "STANDARD-1", "", "§2023:13.18.43.4 GR5")]
    [InlineData(85, "LINE-SEQUENTIAL", "\n           RECORD CONTAINS 10 CHARACTERS", "§1985:VII.3.8.4 GR3")]
    [InlineData(2023, "LINE-SEQUENTIAL", "\n           RECORD CONTAINS 10 CHARACTERS", "§2023:13.18.43.4 GR6")]
    public void OnFixedLengthRecords_IsRefused(int edition, string operand, string fd, string cited) =>
        AssertRefused(Program($"               RECORD DELIMITER IS {operand}", fd), edition, OnFixedLength, cited);

    /// <summary>SR1's three ways to variable-length records, each accepted: RECORD IS VARYING, RECORD CONTAINS integer TO
    /// integer (Annex A.1 item 148), and a line sequential file with no RECORD clause (§2023:9.1.7.2) — here made line
    /// sequential by the clause itself, and at 2023 by its ORGANIZATION clause.</summary>
    [Theory]
    [InlineData(85, "               RECORD DELIMITER IS BINARY-SEQUENTIAL", Varying)]
    [InlineData(85, "               RECORD DELIMITER IS BINARY-SEQUENTIAL", "\n           RECORD CONTAINS 5 TO 10 CHARACTERS")]
    [InlineData(2023, "               RECORD DELIMITER IS BINARY-SEQUENTIAL", "\n           RECORD CONTAINS 5 TO 10 CHARACTERS")]
    [InlineData(85, "               RECORD DELIMITER IS LINE-SEQUENTIAL", "")]
    [InlineData(2023, "               RECORD DELIMITER IS LINE-SEQUENTIAL", "")]
    [InlineData(2023, "               ORGANIZATION IS LINE SEQUENTIAL\n               RECORD DELIMITER IS LINE-SEQUENTIAL", "")]
    public void OnVariableLengthRecords_Compiles(int edition, string selectTail, string fd) =>
        AssertCompiles(Program(selectTail, fd), edition);

    /// <summary>BINARY-SEQUENTIAL names the length prefix, a method a file whose ORGANIZATION clause writes LINE
    /// SEQUENTIAL cannot have, so it is refused there; the clause order does not matter.</summary>
    [Theory]
    [InlineData("               ORGANIZATION IS LINE SEQUENTIAL\n               RECORD DELIMITER IS BINARY-SEQUENTIAL")]
    [InlineData("               RECORD DELIMITER IS BINARY-SEQUENTIAL\n               ORGANIZATION IS LINE SEQUENTIAL")]
    public void BinarySequential_OnALineSequentialOrganization_IsRefused(string selectTail) =>
        AssertRefused(Program(selectTail, ""), 2023, NameUndefined, "names the record sequential method");

    /// <summary>A file made line sequential by the clause writes no LINE SEQUENTIAL phrase, so the two rules whose
    /// subject is that phrase (§2023:12.4.5.2 SR12 on RESERVE, §2023:13.4.5.3 SR4 on BLOCK CONTAINS) do not reach it: a
    /// 1985 program keeps its RESERVE and BLOCK CONTAINS clauses.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void LineSequentialByDelimiter_KeepsReserveAndBlockContains(int edition) =>
        AssertCompiles(Program("               RESERVE 2 AREAS\n               RECORD DELIMITER IS LINE-SEQUENTIAL",
            "\n           BLOCK CONTAINS 2 RECORDS"), edition);

    /// <summary>A file made line sequential at --std 85 is a file of the 1985 Sequential I-O module, so its READ owes the
    /// AT END phrase when no USE procedure applies (§1985:VII.4.4.3 SR2; COBOL6826): the line sequential organization
    /// does not exempt it. With the phrase written it compiles.</summary>
    [Fact]
    public void LineSequentialByDelimiter_At85_ReadOwesAtEnd()
    {
        const string select = "               RECORD DELIMITER IS LINE-SEQUENTIAL";
        AssertRefused(Program(select, "", body: "           OPEN INPUT F1\n           READ F1\n           CLOSE F1\n"),
            85, "COBOL6826", "§1985:VII.4.4.3 SR2");
        AssertCompiles(Program(select, "",
            body: "           OPEN INPUT F1\n           READ F1 AT END CONTINUE END-READ\n           CLOSE F1\n"), 85);
    }

    /// <summary>A line sequential file has no backward walk (§2023:14.9.30.3 SR7 denies it the PREVIOUS phrase), so OPEN
    /// REVERSED, which COBOL-85 admits for a record sequential file, is refused on a file made line sequential at --std 85
    /// (COBOL2210), and still compiles on its BINARY-SEQUENTIAL twin.</summary>
    [Fact]
    public void LineSequentialByDelimiter_At85_RefusesReversed()
    {
        const string body = "           OPEN INPUT F1 REVERSED\n           CLOSE F1\n";
        AssertRefused(Program("               RECORD DELIMITER IS LINE-SEQUENTIAL", "", body: body), 85, "COBOL2210");
        AssertCompiles(Program("               RECORD DELIMITER IS BINARY-SEQUENTIAL", Varying, body: body), 85);
    }

    /// <summary>Neither name is a reserved word (§1985:IV.4.2.2.1, §2023:8.3.2.1 SR4: a system-name and a user-defined
    /// word may be the same word): a data item spelled LINE-SEQUENTIAL and one spelled BINARY-SEQUENTIAL compile, beside
    /// a clause that names one of them.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void Names_AreNotReservedWords(int edition) =>
        AssertCompiles(Program("               RECORD DELIMITER IS BINARY-SEQUENTIAL", Varying,
            records: "       01 R1 PIC X(10).\n",
            body: "           MOVE R1 TO R1\n           CLOSE F1\n")
            .Replace("       01 WS-LEN PIC 99.\n",
                "       01 LINE-SEQUENTIAL PIC X.\n       01 BINARY-SEQUENTIAL PIC X.\n", StringComparison.Ordinal), edition);
}
