// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// The one program shape and the one compile the parse-recovery tests of <c>CobolErrorStrategy</c> share (kb/Work
/// PB3843, PB4133, PB4134, PB4173: <see cref="ParseRecoveryTailTests"/>, <see cref="ParseRecoveryDeletionTests"/>,
/// <see cref="ParseRecoveryUnitTailTests"/>): a free-form program with the files, the sort file and the table every statement under test
/// names, and the errors of its check-only compile. Each test asserts the COUNT of diagnostics, never that a code is
/// present among others.
/// </summary>
internal static class RecoverySources
{
    /// <summary>A program whose file descriptions carry <paramref name="f1Clauses"/> after <c>FD F1</c>, whose
    /// working-storage ends with <paramref name="workingStorage"/> (entries), and whose paragraph MAIN-PARA holds
    /// <paramref name="statements"/> and STOP RUN.</summary>
    public static string Program(string statements, string workingStorage = "", string f1Clauses = "") => $$"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PRREC.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F1 ASSIGN TO "PRREC1.DAT".
            SELECT F2 ASSIGN TO "PRREC2.DAT".
            SELECT F3 ASSIGN TO "PRREC3.DAT".
            SELECT SW ASSIGN TO "PRREC.TMP".
        DATA DIVISION.
        FILE SECTION.
        FD F1 {{f1Clauses}}.
        01 R1 PIC X(10).
        FD F2.
        01 R2 PIC X(10).
        FD F3.
        01 R3 PIC X(10).
        SD SW.
        01 SW-REC.
           05 SW-K PIC X(4).
           05 SW-D PIC X(6).
        WORKING-STORAGE SECTION.
        01 W-A PIC X(10) VALUE "A,B".
        01 W-B PIC X(10).
        01 W-N PIC 9(4) VALUE 0.
        01 W-K PIC 9(4) VALUE 0.
        01 T.
           05 T-E PIC X OCCURS 5 INDEXED BY IX.
        {{workingStorage}}
        PROCEDURE DIVISION.
        MAIN-PARA.
            {{statements}}
            STOP RUN.

        """;

    /// <summary>The edition a recovery test compiles at unless it names one: the compiler's default, COBOL-2023.</summary>
    public const int DefaultEdition = 2023;

    /// <summary>The errors of the check-only compile of <paramref name="source"/> (free form, at
    /// <paramref name="edition"/>, <see cref="DefaultEdition"/> unless named: a sweep over the editions that reserve a
    /// word passes each, kb/Work PB4132); the source holds a syntax error and must be rejected.</summary>
    public static IReadOnlyList<string> Errors(string source, int edition = DefaultEdition)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PBREC_");
        try
        {
            string src = Path.Combine(dir, "prrec.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "prrec.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Free));
            Assert.False(r.Success, "the source holds a syntax error and must be rejected");
            return r.Errors;
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>The diagnostics, in order, as a message for a failed assertion.</summary>
    public static string Listing(IReadOnlyList<string> errors) => string.Join("\n", errors);
}
