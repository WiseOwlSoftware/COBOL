// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB162 — the <c>--permissive</c> lane for the vendor spellings of a FORMAT-1 CALL. ISO §14.9.4.2
/// Format 1 (<c>cite.py --check 14.9.4.2 "Format 1 (Program)"</c> → OK) prints <c>[BY REFERENCE] {identifier-2}</c>
/// and <c>BY CONTENT {identifier-2}</c> and nothing else: no BY VALUE, no literal, no expression. Those belong to
/// Format 2, which the AS phrase selects. Strict keeps PB130's ISO-correct narrowing (COBOLNET0899, an error);
/// <c>--permissive</c> reports the same code as a WARNING and binds the argument with the Format-2 semantics the
/// spelling plainly intends — a literal or expression crosses as a detached BY CONTENT value, BY VALUE as the Value
/// mode. GnuCOBOL accepts all of these on a CALL naming no prototype, and IBM / Micro Focus accept BY VALUE and a
/// BY CONTENT literal (CLAUDE.md rule 1's precedence for implementor latitude).
/// </summary>
public sealed class FormatOneVendorCallPermissiveTests
{
    // Expected values, derived: the callee's formals occupy detached copies (a literal / expression / BY VALUE
    // argument has no storage of the caller's to alias), so VSUB1's MOVE into B never reaches anything and N keeps
    // its 2. A=abcd is the BY REFERENCE identifier; B= is the literal's own characters; V= the value passed.
    private const string Program = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB162VEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "abcd".
       01 N PIC S9(9) COMP-5 VALUE 2.
       PROCEDURE DIVISION.
           CALL "PB162S1" USING X BY CONTENT "WXYZ"
           CALL "PB162S1" USING X "QRST"
           CALL "PB162S2" USING BY VALUE N
           CALL "PB162S2" USING BY VALUE 5
           CALL "PB162S2" USING BY VALUE N + 1
           CALL "PB162S3" USING BY CONTENT 7
           DISPLAY "N=" N
           STOP RUN.
       END PROGRAM PB162VEND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB162S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(4).
       01 B PIC X(4).
       PROCEDURE DIVISION USING A B.
           DISPLAY "A=" A " B=" B
           MOVE "ZZZZ" TO B
           GOBACK.
       END PROGRAM PB162S1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB162S2.
       DATA DIVISION.
       LINKAGE SECTION.
       01 V PIC S9(9) COMP-5.
       PROCEDURE DIVISION USING BY VALUE V.
           DISPLAY "V=" V
           ADD 1 TO V
           GOBACK.
       END PROGRAM PB162S2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB162S3.
       DATA DIVISION.
       LINKAGE SECTION.
       01 D PIC X.
       PROCEDURE DIVISION USING D.
           DISPLAY "D=" D
           GOBACK.
       END PROGRAM PB162S3.
       """;

    /// <summary>Strict: each of the six vendor spellings is the standard's error — the narrowing is unchanged.</summary>
    [Fact]
    public void VendorSpellings_AreRejectedUnderStrict()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program, 2023);
        Assert.False(ok, "ISO §14.9.4.2 Format 1 admits identifier-2 only");
        Assert.Equal(6, errors.Count(e => e.Contains("COBOLNET0899")));
    }

    /// <summary><c>--permissive</c>: six warnings under the same code, and each argument binds as the Format-2
    /// spelling it stands for.</summary>
    [Fact]
    public void VendorSpellings_UnderPermissive_WarnAndBindWithFormatTwoSemantics()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Program, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept the vendor spellings: {string.Join("\n", errors)}");
        Assert.Equal(6, warnings.Count(w => w.Contains("COBOLNET0899") && w.Contains("--permissive")));

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Program, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(
            new[]
            {
                "A=abcd B=WXYZ",   // BY CONTENT literal — a detached BY CONTENT value
                "A=abcd B=QRST",   // bare literal — the same
                "V=000000002",     // BY VALUE identifier — the Value mode
                "V=000000005",     // BY VALUE literal
                "V=000000003",     // BY VALUE expression N + 1
                "D=7",             // BY CONTENT numeric literal into a PIC X formal of its length
                "N=000000002",     // no BY VALUE store reached the caller's N
            },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }
}
