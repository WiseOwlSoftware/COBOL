// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB5496 — a SORT or MERGE the binder refuses loses its INPUT/OUTPUT PROCEDURE ranges, so the legal RELEASE or RETURN written in
/// the procedure must not be reported as outside every procedure (§1985:XI.4.2.3 SR2, §1985:XI.4.3.3 SR3; COBOL7081): the refusal is the
/// statement's one error, and the unrelated RELEASE/RETURN of ANOTHER file is still judged. A diagnostic list that must NOT carry a code is
/// what a conformance golden's <c>expect</c> cannot say, so it is asserted here.
/// </summary>
public sealed class RefusedSortMergeReleaseReturnTests : IDisposable
{
    private readonly string _dir = ScratchDirectory.Create("WiseOwlCobol_PB5496_");

    public void Dispose() { ScratchDirectory.Remove(_dir); }

    private const string Prologue =
        "       IDENTIFICATION DIVISION.\n" +
        "       PROGRAM-ID. PB5496T.\n" +
        "       ENVIRONMENT DIVISION.\n" +
        "       INPUT-OUTPUT SECTION.\n" +
        "       FILE-CONTROL.\n" +
        "           SELECT SW ASSIGN TO \"pb5496-sw.tmp\".\n" +
        "           SELECT SW2 ASSIGN TO \"pb5496-sw2.tmp\".\n" +
        "           SELECT F1 ASSIGN TO \"pb5496-f1.dat\".\n" +
        "           SELECT F2 ASSIGN TO \"pb5496-f2.dat\".\n" +
        "           SELECT F3 ASSIGN TO \"pb5496-f3.dat\".\n" +
        "       DATA DIVISION.\n" +
        "       FILE SECTION.\n" +
        "       SD SW.\n" +
        "       01 SR.\n" +
        "          05 SK PIC X(4).\n" +
        "       SD SW2.\n" +
        "       01 SR2.\n" +
        "          05 SK2 PIC X(4).\n" +
        "       FD F1.\n" +
        "       01 R1 PIC X(4).\n" +
        "       FD F2.\n" +
        "       01 R2 PIC X(4).\n" +
        "       FD F3.\n" +
        "       01 R3 PIC X(4).\n" +
        "       PROCEDURE DIVISION.\n" +
        "       MAIN-PARA.\n";

    private List<string> Compile(string body)
    {
        string path = Path.Combine(_dir, "t" + Guid.NewGuid().ToString("N")[..6] + ".cob");
        File.WriteAllText(path, Prologue + body);
        var r = CompiledProgramCache.Compile(new CompilerDriver.Options(
            path, EditionInfo.Of(85), Path.Combine(_dir, "out.dll"), CheckOnly: true,
            CopyPaths: [_dir], SourceFormat: InitialReferenceFormat.Fixed));
        Assert.False(r.Success);
        return [.. r.Errors, .. r.Warnings];
    }

    [Fact] // MERGE USING F1 F1: SR7 (a file repeated) refuses the statement; the RETURN in its output procedure is legal
    public void RefusedMerge_DoesNotReportItsOutputProceduresReturn()
    {
        var lines = Compile(
            "           MERGE SW ASCENDING KEY SK USING F1 F1 OUTPUT PROCEDURE IS OP.\n" +
            "           STOP RUN.\n" +
            "       OP.\n" +
            "           RETURN SW AT END DISPLAY \"E\".\n");
        Assert.Contains(lines, d => d.Contains("is repeated"));
        Assert.DoesNotContain(lines, d => d.Contains("COBOL7081"));
    }

    [Fact] // SORT ... INPUT PROCEDURE IS IP GIVING SW2 (an SD cannot be a GIVING file): the same for the RELEASE of the refused SORT's input procedure
    public void RefusedSort_DoesNotReportItsInputProceduresRelease()
    {
        var lines = Compile(
            "           SORT SW ASCENDING KEY SK INPUT PROCEDURE IS IP GIVING SW2.\n" +
            "           STOP RUN.\n" +
            "       IP.\n" +
            "           RELEASE SR.\n");
        Assert.DoesNotContain(lines, d => d.Contains("COBOL7081"));
        Assert.NotEmpty(lines);
    }

    [Fact] // the refusal silences only its own file: a RETURN of SW2 outside every procedure is still COBOL7081
    public void RefusedMerge_StillReportsAReturnOfAnotherFile()
    {
        var lines = Compile(
            "           MERGE SW ASCENDING KEY SK USING F1 F1 OUTPUT PROCEDURE IS OP.\n" +
            "           STOP RUN.\n" +
            "       OP.\n" +
            "           RETURN SW AT END DISPLAY \"E\".\n" +
            "       STRAY.\n" +
            "           RETURN SW2 AT END DISPLAY \"E\".\n");
        Assert.Single(lines, d => d.Contains("COBOL7081"));
        Assert.Contains(lines, d => d.Contains("'SW2'") && d.Contains("COBOL7081"));
    }

    [Fact] // kb/Work PB6068: the guard is per statement, not per file - a RELEASE of the refused SORT's own file written in a plain paragraph is still COBOL7081
    public void RefusedSort_StillReportsAReleaseOutsideItsRange()
    {
        var lines = Compile(
            "           SORT SW ASCENDING KEY SK INPUT PROCEDURE IS IP GIVING SW2.\n" +
            "           STOP RUN.\n" +
            "       IP.\n" +
            "           RELEASE SR.\n" +
            "       STRAY.\n" +
            "           RELEASE SR.\n");
        Assert.Single(lines, d => d.Contains("COBOL7081"));
    }

    [Fact] // kb/Work PB6068: a refused MERGE whose output procedure range holds the RETURN, with a second RETURN of the same file elsewhere
    public void RefusedMerge_StillReportsAReturnOutsideItsRange()
    {
        var lines = Compile(
            "           MERGE SW ASCENDING KEY SK USING F1 F1 OUTPUT PROCEDURE IS OP.\n" +
            "           STOP RUN.\n" +
            "       OP.\n" +
            "           RETURN SW AT END DISPLAY \"E\".\n" +
            "       STRAY.\n" +
            "           RETURN SW AT END DISPLAY \"E\".\n");
        Assert.Single(lines, d => d.Contains("COBOL7081"));
    }

    [Fact] // kb/Work PB6068: a SORT refused before its procedure names are read (no KEY phrase) still has its range, so the legal RELEASE stays quiet
    public void RefusedSortBeforeItsProcedures_KeepsItsInputRange()
    {
        var lines = Compile(
            "           SORT SW INPUT PROCEDURE IS IP GIVING F1.\n" +
            "           STOP RUN.\n" +
            "       IP.\n" +
            "           RELEASE SR.\n");
        Assert.DoesNotContain(lines, d => d.Contains("COBOL7081"));
        Assert.NotEmpty(lines);
    }

    [Fact] // a phrase naming no procedure leaves the range unknown, so the file's uses of that direction are not judged (the one case that stays file-wide)
    public void RefusedSortWithAnUnknownProcedure_JudgesNoReleaseOfItsFile()
    {
        var lines = Compile(
            "           SORT SW ASCENDING KEY SK INPUT PROCEDURE IS NOPE GIVING F1.\n" +
            "           STOP RUN.\n" +
            "       STRAY.\n" +
            "           RELEASE SR.\n");
        Assert.DoesNotContain(lines, d => d.Contains("COBOL7081"));
        Assert.NotEmpty(lines);
    }
}
