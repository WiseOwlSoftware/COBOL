// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// A READ with a KEY phrase is a Format 2 (random) READ, and the diagnostic that refuses one on an indexed file
/// names what the statement actually does wrong (kb/Work PB3569). ISO §14.9.30.2 prints the KEY phrase in Format
/// 2 only, and Format 2 prints none of ADVANCING, AT END, NEXT, NOT AT END or PREVIOUS; §14.9.30.4 GR19: "An
/// implicit or explicit NEXT phrase or a PREVIOUS phrase results in a sequential read: otherwise, the read is a
/// random read and the rules for format 2 apply." Two spellings are refused, each with its own message:
/// <list type="bullet">
/// <item>a sequential-read phrase written beside the KEY phrase: the message names every such phrase the statement
/// wrote and no other;</item>
/// <item>no such phrase, on a file whose access mode is SEQUENTIAL (written, or assumed when the ACCESS MODE clause
/// is absent, §12.4.5.5.3 GR1): §14.9.30.3 SR8 implies the NEXT phrase, GR19 makes an implicit NEXT a sequential
/// read and a sequential read has no KEY phrase. The statement has no wrong phrase, the file is the wrong mode for
/// it, so the message names the access mode and the implied NEXT and no phrase the statement wrote. That is the
/// reading the binder's <c>ReadKind.Next</c> and the INVALID KEY screen already use; the same SR8 + GR19 reading
/// refuses INVALID KEY on a READ that binds sequential (COBOL1720), pinned below.</item></list>
/// An ACCESS MODE RANDOM or DYNAMIC file reads by key (GR3, GR4) and is accepted. The statement words AT END and NOT
/// AT END beside a KEY phrase never reach the binder: the parser refuses them (COBOL2269, kb/Work PB3830, golden
/// <c>14.5.3.3-Implicit-scope-termination/pb3830-read-key-at-end</c>).
/// </summary>
public sealed class ReadKeyFormatTests
{
    private const string AccessModeNotSpecified = "the ACCESS MODE clause is not specified, so sequential access is assumed";

    /// <summary>The words that make a READ sequential: a message about the file's access mode must not name them.</summary>
    private static readonly string[] SequentialReadWords = ["ADVANCING", "AT END", "NEXT", "PREVIOUS"];

    [Theory]
    [InlineData(85, "")]
    [InlineData(2002, "")]
    [InlineData(2014, "")]
    [InlineData(2023, "")]
    [InlineData(85, "ACCESS MODE IS SEQUENTIAL")]
    [InlineData(2002, "ACCESS MODE IS SEQUENTIAL")]
    [InlineData(2014, "ACCESS MODE IS SEQUENTIAL")]
    [InlineData(2023, "ACCESS MODE IS SEQUENTIAL")]
    public void AReadKeyOnASequentialAccessFile_NamesTheAccessModeAndNoPhrase(int edition, string accessClause)
    {
        var (ok, diagnostics) = EditionHarness.Compile(
            Program("PBRK01", accessClause, "READ F1 KEY IS F1-K INVALID KEY DISPLAY \"I\" END-READ"), edition);
        Assert.False(ok, "a Format 2 READ ... KEY on a SEQUENTIAL access file compiled");
        string message = Single0864(diagnostics);
        Assert.Contains("the file's access mode is SEQUENTIAL", message);
        // ONE reading of SR8 + GR19, the binder's and the INVALID KEY screen's: the implied NEXT makes it sequential.
        Assert.Contains("so the NEXT phrase is implied (ISO §14.9.30.3 SR8) and the READ is a sequential read (ISO §14.9.30.4 GR19)", message);
        Assert.DoesNotContain("random READ (ISO §14.9.30.4 GR19)", message);   // GR19 is not the rule that makes THIS a random read
        Assert.Contains("ACCESS MODE IS RANDOM, or DYNAMIC", message);
        if (accessClause.Length == 0) Assert.Contains(AccessModeNotSpecified, message);
        else Assert.DoesNotContain(AccessModeNotSpecified, message);
        Assert.DoesNotContain("this statement specifies", message);   // the phrase arm's wording: it wrote none
        foreach (string word in SequentialReadWords.Where(w => w != "NEXT"))
            Assert.DoesNotContain(word, message);   // the statement wrote none of them; NEXT appears only as IMPLIED
    }

    [Theory]
    [InlineData(85, "")]
    [InlineData(2002, "ACCESS MODE IS SEQUENTIAL")]
    [InlineData(2014, "")]
    [InlineData(2023, "ACCESS MODE IS SEQUENTIAL")]
    public void AReadInvalidKeyOnASequentialAccessFile_IsRefusedByTheSameReadingAsAKeyPhrase(int edition, string accessClause)
    {
        // The sibling arm (review lead R1-2): INVALID KEY is Format 2 only too, and the sequential-organization READ
        // already refuses it by SR8 + GR19 (COBOL1720); a keyed file whose access mode is sequential is the same READ.
        string source = Program("PBRK05", accessClause, "READ F1 INVALID KEY DISPLAY \"I\" END-READ");
        var (ok, diagnostics) = EditionHarness.Compile(source, edition);
        Assert.False(ok, "a Format-2-only INVALID KEY phrase on a sequential READ compiled under strict");
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOL1720");
        Assert.DoesNotContain(diagnostics, d => d.Contains("COBOL0864", StringComparison.Ordinal));   // no KEY phrase written
        // --permissive: the CCVS leniency seam, a warning with the bind unchanged (the INVALID arm is dead).
        var (permissiveOk, errors, warnings) = EditionHarness.CompileFull(source, edition, permissive: true);
        Assert.True(permissiveOk, $"--permissive must tolerate the phrase; got:\n{string.Join("\n", errors)}");
        EditionHarness.AssertHasDiagnostic(warnings, "COBOL1720");
    }

    [Theory]
    [InlineData(85, "ACCESS MODE IS RANDOM")]
    [InlineData(2023, "ACCESS MODE IS RANDOM")]
    [InlineData(85, "ACCESS MODE IS DYNAMIC")]
    [InlineData(2023, "ACCESS MODE IS DYNAMIC")]
    public void AReadInvalidKeyWithoutANextPhraseOnARandomOrDynamicFile_IsAccepted(int edition, string accessClause)
    {
        // Dynamic access with none of ADVANCING / AT END / NOT AT END is the Format-2 random read (SR9): its INVALID KEY is legal.
        var (ok, diagnostics) = EditionHarness.Compile(
            Program("PBRK06", accessClause, "READ F1 INVALID KEY DISPLAY \"I\" END-READ"), edition);
        Assert.True(ok, $"a Format 2 READ ... INVALID KEY on an {accessClause} file is legal; got:\n{string.Join("\n", diagnostics)}");
    }

    [Theory]
    [InlineData(85, "ACCESS MODE IS DYNAMIC", "READ F1 NEXT KEY IS F1-K", "NEXT")]
    [InlineData(85, "ACCESS MODE IS SEQUENTIAL", "READ F1 NEXT RECORD KEY IS F1-K", "NEXT")]
    [InlineData(2002, "ACCESS MODE IS DYNAMIC", "READ F1 NEXT KEY IS F1-K", "NEXT")]
    [InlineData(2002, "ACCESS MODE IS DYNAMIC", "READ F1 PREVIOUS KEY IS F1-K", "PREVIOUS")]
    [InlineData(2002, "ACCESS MODE IS DYNAMIC", "READ F1 ADVANCING ON LOCK KEY IS F1-K", "ADVANCING")]
    [InlineData(2014, "ACCESS MODE IS DYNAMIC", "READ F1 PREVIOUS ADVANCING ON LOCK KEY IS F1-K", "ADVANCING / PREVIOUS")]
    [InlineData(2023, "ACCESS MODE IS SEQUENTIAL", "READ F1 PREVIOUS KEY IS F1-K", "PREVIOUS")]
    [InlineData(2023, "ACCESS MODE IS DYNAMIC", "READ F1 NEXT KEY IS F1-K", "NEXT")]
    public void AReadKeyBesideASequentialReadPhrase_NamesExactlyThePhrasesWritten(
        int edition, string accessClause, string statement, string written)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program("PBRK02", accessClause, statement + " END-READ"), edition);
        Assert.False(ok, $"'{statement}' compiled");
        string message = Single0864(diagnostics);
        Assert.Contains($"this statement specifies {written};", message);
        Assert.Contains("whose general format prints none of the phrases ADVANCING, AT END, NEXT, NOT AT END or PREVIOUS (ISO §14.9.30.2)", message);
        Assert.DoesNotContain("access mode is SEQUENTIAL", message);   // the file's mode is not what is wrong here
    }

    [Theory]
    [InlineData(85, "ACCESS MODE IS RANDOM")]
    [InlineData(85, "ACCESS MODE IS DYNAMIC")]
    [InlineData(2002, "ACCESS MODE IS RANDOM")]
    [InlineData(2002, "ACCESS MODE IS DYNAMIC")]
    [InlineData(2014, "ACCESS MODE IS RANDOM")]
    [InlineData(2014, "ACCESS MODE IS DYNAMIC")]
    [InlineData(2023, "ACCESS MODE IS RANDOM")]
    [InlineData(2023, "ACCESS MODE IS DYNAMIC")]
    public void AReadKeyOnARandomOrDynamicAccessFile_IsAccepted(int edition, string accessClause)
    {
        var (ok, diagnostics) = EditionHarness.Compile(
            Program("PBRK03", accessClause, "READ F1 KEY IS F1-K INVALID KEY DISPLAY \"I\" END-READ"), edition);
        Assert.True(ok, $"a Format 2 READ ... KEY on an {accessClause} file is legal (§12.4.5.5.3 GR3, GR4); got:\n{string.Join("\n", diagnostics)}");
    }

    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    [InlineData(2023)]
    public void AReadNextKeyOnARandomAccessFile_IsNamedOnceBySyntaxRule6(int edition)
    {
        // SR6 already names NEXT against ACCESS MODE RANDOM (COBOL1720); the KEY check does not say it a second time.
        var (ok, diagnostics) = EditionHarness.Compile(
            Program("PBRK04", "ACCESS MODE IS RANDOM", "READ F1 NEXT KEY IS F1-K END-READ"), edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOL1720");
        Assert.DoesNotContain(diagnostics, d => d.Contains("COBOL0864", StringComparison.Ordinal));
    }

    private static string Single0864(IReadOnlyList<string> diagnostics)
    {
        var found = diagnostics.Where(d => d.Contains("COBOL0864", StringComparison.Ordinal)).ToList();
        Assert.True(found.Count == 1, $"expected exactly one COBOL0864; got:\n{string.Join("\n", diagnostics.DefaultIfEmpty("(none)"))}");
        return found[0];
    }

    /// <summary>An INDEXED file F1 with <paramref name="accessClause"/> (omitted when empty) read by
    /// <paramref name="statement"/>; the program is compiled, never run.</summary>
    private static string Program(string pid, string accessClause, string statement) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "{pid}1.DAT"
                ORGANIZATION IS INDEXED
                {accessClause}
                RECORD KEY IS F1-K.
        DATA DIVISION.
        FILE SECTION.
        FD F1.
        01 R1.
           05 F1-K PIC X(3).
           05 F1-D PIC X(3).
        PROCEDURE DIVISION.
        MAIN-PARA.
            OPEN INPUT F1
            {statement}
            CLOSE F1
            STOP RUN.

        """;
}
