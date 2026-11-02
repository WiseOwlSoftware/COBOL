// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB162 — the <c>--permissive</c> lane for the vendor spellings of a FORMAT-1 CALL. ISO §14.9.4.2
/// Format 1 (<c>cite.py --check 14.9.4.2 "Format 1 (Program)"</c> → OK) prints <c>[BY REFERENCE] {identifier-2}</c>
/// and <c>BY CONTENT {identifier-2}</c> and nothing else: no BY VALUE, no literal, no expression. Those belong to
/// Format 2, which the AS phrase selects. Strict keeps PB130's ISO-correct narrowing (COBOL0899, an error);
/// <c>--permissive</c> reports the same code as a WARNING and binds the argument with the Format-2 semantics the
/// spelling plainly intends — a literal or expression crosses as a detached BY CONTENT value, BY VALUE as the Value
/// mode. GnuCOBOL accepts all of these on a CALL naming no prototype, and IBM / Micro Focus accept BY VALUE and a
/// BY CONTENT literal (CLAUDE.md rule 1's precedence for implementor latitude). OMITTED passed BY REFERENCE is the
/// sixth vendor spelling (kb/Work PB4012); docs/CONFORMANCE.md D-CALLV records the survey and the owner's decision.
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
        Assert.Equal(6, errors.Count(e => e.Contains("COBOL0899")));
    }

    /// <summary><c>--permissive</c>: six warnings under the same code, and each argument binds as the Format-2
    /// spelling it stands for.</summary>
    [Fact]
    public void VendorSpellings_UnderPermissive_WarnAndBindWithFormatTwoSemantics()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(Program, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept the vendor spellings: {string.Join("\n", errors)}");
        Assert.Equal(6, warnings.Count(w => w.Contains("COBOL0899") && w.Contains("--permissive")));

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(Program, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(
            new[]
            {
                "A=abcd B=WXYZ",   // BY CONTENT literal — a detached BY CONTENT value
                "A=abcd B=QRST",   // bare literal — the same
                "V=+0000000002",    // BY VALUE identifier — the Value mode (a COMP-5 item shows its 4-byte container's ten digits, signed)
                "V=+0000000005",    // BY VALUE literal
                "V=+0000000003",    // BY VALUE expression N + 1
                "D=7",             // BY CONTENT numeric literal into a PIC X formal of its length
                "N=+0000000002",    // no BY VALUE store reached the caller's N
            },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    // kb/Work PB4012 (owner decision 2026-10-28: "Yes, permissive accepts"): OMITTED passed BY REFERENCE is the sixth
    // vendor spelling. Expected values, derived: §14.9.4.4 GR11 makes an omitted argument's formal OMITTED, so the
    // callee's §8.8.4.8 omitted-argument condition `B IS OMITTED` is true for the first two calls and false for the
    // third, which passes W in both positions.
    private const string OmittedProgram = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4012VEND.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           CALL "PB4012S1" USING W OMITTED
           CALL "PB4012S1" USING W BY REFERENCE OMITTED
           CALL "PB4012S1" USING W W
           STOP RUN.
       END PROGRAM PB4012VEND.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4012S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 A PIC X(4).
       01 B PIC X(4).
       PROCEDURE DIVISION USING A OPTIONAL B.
           IF B IS OMITTED
               DISPLAY "A=" A " B OMITTED"
           ELSE
               DISPLAY "A=" A " B=" B
           END-IF
           GOBACK.
       END PROGRAM PB4012S1.
       """;

    /// <summary>Strict: both OMITTED spellings are the standard's COBOL0899 (kb/Work PB3996, unchanged).</summary>
    [Fact]
    public void OmittedByReference_IsRejectedUnderStrict()
    {
        var (ok, errors, _) = EditionHarness.CompileFull(OmittedProgram, 2023);
        Assert.False(ok, "ISO §14.9.4.2 Format 1 prints OMITTED nowhere");
        Assert.Equal(2, errors.Count(e => e.Contains("COBOL0899") && e.Contains("OMITTED")));
    }

    /// <summary><c>--permissive</c>: one vendor-extension warning per OMITTED, naming it, and each binds as the
    /// §14.9.4.4 GR11 omitted argument.</summary>
    [Fact]
    public void OmittedByReference_UnderPermissive_WarnsAndPassesTheOmittedArgument()
    {
        var (ok, errors, warnings) = EditionHarness.CompileFull(OmittedProgram, 2023, permissive: true);
        Assert.True(ok, $"--permissive must accept OMITTED passed BY REFERENCE: {string.Join("\n", errors)}");
        Assert.Equal(2, warnings.Count(w => w.Contains("COBOL0899") && w.Contains("OMITTED") && w.Contains("--permissive")));

        var (ranOk, stdout, detail) = EditionHarness.CompileAndRun(OmittedProgram, 2023, permissive: true);
        Assert.True(ranOk, detail);
        Assert.Equal(new[] { "A=ABCD B OMITTED", "A=ABCD B OMITTED", "A=ABCD B=ABCD" },
            stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n'));
    }

    /// <summary>An OMITTED that a BY CONTENT phrase carries over (§14.9.4.4 GR5) has no vendor lane: GnuCOBOL refuses
    /// it too (cobc 3.2, kb/Work PB3996), so <c>--permissive</c> keeps the error.</summary>
    [Fact]
    public void OmittedAfterByContent_StaysRejectedUnderPermissive()
    {
        const string source = """
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4012BC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(4) VALUE "ABCD".
       PROCEDURE DIVISION.
           CALL "OTHER" USING BY CONTENT W OMITTED
           STOP RUN.
       """;
        var (ok, errors, _) = EditionHarness.CompileFull(source, 2023, permissive: true);
        Assert.False(ok, "an omitted argument is passed BY REFERENCE only");
        Assert.Single(errors, e => e.Contains("COBOL0899") && e.Contains("OMITTED (after BY CONTENT)"));
    }
}
