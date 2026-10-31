// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A FILE OF A CONTAINING ELEMENT IN AN APPLY COMMIT CLAUSE IS SR1's OR SR11's VIOLATION, NEVER AN UNDEFINED DATA-NAME
/// (kb/Work PB3557). ISO §12.4.6.3.3 SR1: <i>"File-name-1 shall be specified in the FILE-CONTROL paragraph of the source
/// element that contains this clause"</i>; SR11: <i>"Where a file or record area is declared with the GLOBAL clause, it
/// may only be specified in the APPLY COMMIT clause of the same source element."</i> The clause's bare word is file-name-1
/// only when this element selects it; every other word is identifier-1, so the container's file used to resolve as a
/// data-name, find none and draw COBOL1639 "no declaration in this source element gives the name" — false for a GLOBAL
/// file, visible here by §13.18.27.4 GR1 — while the COBOL4709 descriptor claimed an SR1 report no code path made.
/// <c>DataBinder.ScreenEnclosingFileOperand</c> reports it under its own rule; <c>DataBinder.NonDataDeclarationOf</c>
/// answers "a file-name" to every other site that wants a data item (the wording, not the code, changes there).
/// </summary>
public sealed class ApplyCommitEnclosingFileTests
{
    /// <summary>A containing program with file RELF (GLOBAL or not) and a contained program whose I-O-CONTROL paragraph
    /// holds <paramref name="applyOperand"/>; <paramref name="containedWorking"/> is its WORKING-STORAGE body.</summary>
    private static string Nested(bool globalFile, string applyOperand, string containedWorking = "", bool ownFile = false) =>
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB3557O.\n"
        + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + "           SELECT RELF ASSIGN TO \"pb3557.dat\" ORGANIZATION IS RELATIVE\n"
        + "               ACCESS MODE IS RANDOM RELATIVE KEY IS RK.\n"
        + $"       DATA DIVISION.\n       FILE SECTION.\n       FD RELF{(globalFile ? " IS GLOBAL" : "")}.\n"
        + "       01 RREC PIC X(10).\n       WORKING-STORAGE SECTION.\n       01 RK PIC 9(4) GLOBAL.\n"
        + "       PROCEDURE DIVISION.\n           CALL \"PB3557I\"\n           STOP RUN.\n"
        + "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB3557I.\n"
        + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n"
        + (ownFile ? "       FILE-CONTROL.\n           SELECT RELF ASSIGN TO \"pb3557i.dat\" ORGANIZATION IS RELATIVE\n"
            + "               ACCESS MODE IS RANDOM RELATIVE KEY IS RK.\n" : "")
        + "       I-O-CONTROL.\n"
        + $"           APPLY COMMIT ON {applyOperand}.\n"
        + "       DATA DIVISION.\n"
        + (ownFile ? "       FILE SECTION.\n       FD RELF.\n       01 RREC2 PIC X(10).\n" : "")
        + (containedWorking.Length == 0 ? "" : "       WORKING-STORAGE SECTION.\n" + containedWorking)
        + "       PROCEDURE DIVISION.\n           GOBACK.\n"
        + "       END PROGRAM PB3557I.\n       END PROGRAM PB3557O.\n";

    /// <summary>SR11: the container's GLOBAL file is visible in the contained program, and still may not be named in its
    /// clause. One report, under SR11, the only refusal; the undefined-name verdict is gone.</summary>
    [Fact]
    public void AContainersGlobalFile_IsRefusedUnderSr11()
    {
        var (ok, errors) = EditionHarness.Compile(Nested(globalFile: true, "RELF"), 2023);
        Assert.False(ok);
        var only = Assert.Single(errors);
        Assert.Contains("COBOL4709", only, StringComparison.Ordinal);
        Assert.Contains("ISO §12.4.6.3.3 SR11", only, StringComparison.Ordinal);
        Assert.Contains("GLOBAL", only, StringComparison.Ordinal);
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertNoDiagnostic(errors, "no declaration in this source element");
    }

    /// <summary>SR11's other noun: the container's GLOBAL record area (RREC, subordinate to a GLOBAL FD) is no more
    /// nameable in the contained clause than the file is — and it is not "not defined" either.</summary>
    [Fact]
    public void AContainersGlobalRecordArea_IsRefusedUnderSr11()
    {
        var (ok, errors) = EditionHarness.Compile(Nested(globalFile: true, "RREC"), 2023);
        Assert.False(ok);
        var only = Assert.Single(errors);
        Assert.Contains("COBOL4709", only, StringComparison.Ordinal);
        Assert.Contains("ISO §12.4.6.3.3 SR11", only, StringComparison.Ordinal);
        Assert.Contains("record area", only, StringComparison.Ordinal);
    }

    /// <summary>SR11's record area written with its file-report-qualifier (§8.4.2.2.2 Format 1: <i>"data-name-1 [
    /// data-qualifier ] … [ file-report-qualifier ]"</i>): <c>RREC IN RELF</c> names the same GLOBAL record area as the
    /// bare word, so it meets the same SR11 report, never the undefined-name verdict (kb/Work PB3557 review R1-1: the
    /// screen returned at any qualifier, so the qualified spelling drew COBOL1639).</summary>
    [Fact]
    public void AContainersGlobalRecordArea_QualifiedByItsFile_IsRefusedUnderSr11()
    {
        var (ok, errors) = EditionHarness.Compile(Nested(globalFile: true, "RREC IN RELF"), 2023);
        Assert.False(ok);
        var only = Assert.Single(errors);
        Assert.Contains("COBOL4709", only, StringComparison.Ordinal);
        Assert.Contains("ISO §12.4.6.3.3 SR11", only, StringComparison.Ordinal);
        Assert.Contains("'RREC OF RELF' is a record area of file 'RELF'", only, StringComparison.Ordinal);
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1639");
    }

    /// <summary>THE QUALIFIED ARM THAT CAN FAIL: a qualified word whose name is no GLOBAL record area of the file it
    /// names is no record area at all, and stays an undefined data-name (§8.4.2.1); a qualifier alone never makes SR11.</summary>
    [Fact]
    public void AQualifiedWordNamingNoGlobalRecordArea_IsStillAnUndefinedDataName()
    {
        var (ok, errors) = EditionHarness.Compile(Nested(globalFile: true, "NOPE IN RELF"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL4709");
    }

    /// <summary>SR1: a file with no GLOBAL clause is not visible in the contained program, and is not in its FILE-CONTROL
    /// paragraph either: the clause's own rule, not an undefined name.</summary>
    [Fact]
    public void AContainersNonGlobalFile_IsRefusedUnderSr1()
    {
        var (ok, errors) = EditionHarness.Compile(Nested(globalFile: false, "RELF"), 2023);
        Assert.False(ok);
        var only = Assert.Single(errors);
        Assert.Contains("COBOL4709", only, StringComparison.Ordinal);
        Assert.Contains("ISO §12.4.6.3.3 SR1:", only, StringComparison.Ordinal);
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1639");
    }

    /// <summary>THE ARM THAT PROVES THE PROBE CAN FAIL: a word that is no file anywhere is still an undefined data-name
    /// (§8.4.2.1) — the clause's identifier-1 reading is unchanged.</summary>
    [Fact]
    public void AWordThatIsNoFileAnywhere_IsStillAnUndefinedDataName()
    {
        var (ok, errors) = EditionHarness.Compile(Nested(globalFile: true, "NOPE"), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL4709");
    }

    /// <summary>A contained program's own data item spelled like the container's file hides the file-name (§8.4.6.2.1
    /// 3) a): "If the name is declared in source element B, the item in source element B is the referenced item"), so
    /// the word is identifier-1 as written — no SR1 or SR11 report.</summary>
    [Fact]
    public void ALocalDataItemSpelledLikeTheContainersFile_IsIdentifierOne()
    {
        var (ok, errors) = EditionHarness.Compile(
            Nested(globalFile: true, "RELF", "       01 RELF PIC X(10).\n"), 2023);
        EditionHarness.AssertNoDiagnostic(errors, "COBOL4709");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1639");
        Assert.True(ok, string.Join("\n", errors));
    }

    /// <summary>The contained program's OWN file of the same spelling as the container's GLOBAL file shadows it: the
    /// bare word is this element's file-name-1 (SR1 satisfied), and nothing is refused.</summary>
    [Fact]
    public void AnOwnFileShadowingTheContainersGlobalFile_IsFileNameOne()
    {
        var (ok, errors) = EditionHarness.Compile(Nested(globalFile: true, "RELF", "       01 RK PIC 9(4).\n", ownFile: true), 2023);
        EditionHarness.AssertNoDiagnostic(errors, "COBOL4709");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1639");
        Assert.True(ok, string.Join("\n", errors));
    }
}
