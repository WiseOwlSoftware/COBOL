// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Diagnostics;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN IMPLICIT REDEFINITION IS NOT A REDEFINES CLAUSE (kb/Work PB836).
///
/// <para><b>The rules.</b> ISO §13.18.33.4 GR3 — "Multiple level 1 entries subordinate to a FD or SD entry represent
/// implicit redefinitions of the same area" — and §12.4.6.4.4 GR2, a record-area SAME clause being "equivalent to an
/// implicit redefinition of the area with records aligned on the leftmost byte position". The REDEFINES-clause syntax
/// rules (§13.18.44.3 SR5/SR12/SR14/SR17) are rules about a written clause and its data-name-2 operand; §13.18.57.3
/// SR4 — "the subject of the entry shall not be implicitly or explicitly redefined in whole or in part" — is the one
/// that reaches the implicit case.</para>
///
/// <para><b>What went wrong.</b> The binder realized the shared area by writing <c>RedefinesTarget</c> on the later
/// records and nothing else, so every REDEFINES-clause screen fired on source containing no REDEFINES: a strong
/// record named "data-name-2" under SR14, a <c>USAGE POINTER</c> record refused under SR12 although no rule forbids
/// it. Each member now carries <see cref="DataItem.RedefinesKind"/>, written only with its target.</para>
/// </summary>
public sealed class ImplicitRecordRedefinitionTests
{
    private const string Strong = """
               01 T1 IS TYPEDEF STRONG.
                  05 A PIC X(4).
        """;

    /// <summary>The TWO ARMS of a symmetric sharing: a strong record is implicitly redefined whichever of the two
    /// records the binder anchors the area on, so §13.18.57.3 SR4 fires for both orders — and neither order may
    /// borrow a REDEFINES-clause rule (before the fix the first order drew SR14 + SR4, the second SR12 alone).</summary>
    [Theory]
    [InlineData("01 F-BIG TYPE T1.\n       01 F-OTHER PIC X(4).")]
    [InlineData("01 F-OTHER PIC X(4).\n       01 F-BIG TYPE T1.")]
    public void StrongRecord_InAMultiRecordFd_IsSr4_NeverAClauseRule(string records)
    {
        var (_, ed) = Compile(Fd(records, Strong));
        var sr4 = Assert.Single(ed.Diagnostics, x => x.Contains("COBOLNET1532", StringComparison.Ordinal));
        Assert.Contains("'F-BIG' is a strongly-typed item", sr4);
        Assert.Contains("§13.18.33.4 GR3", sr4);
        Assert.Contains("§13.18.57.3 SR4", sr4);
        Assert.DoesNotContain(ed.Diagnostics, x => x.Contains("§13.18.44", StringComparison.Ordinal));
    }

    /// <summary>§12.4.6.4.4 GR2's implicit redefinition reaches SR4 the same way, and the diagnostic names the
    /// SAME clause as the construct that shares the area.</summary>
    [Fact]
    public void StrongRecord_InASameRecordArea_IsSr4_CitingTheSameClause()
    {
        string src = $"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. IRRSRA.
                   ENVIRONMENT DIVISION.
                   INPUT-OUTPUT SECTION.
                   FILE-CONTROL.
                       SELECT F1 ASSIGN TO "irr_f1.dat" ORGANIZATION IS SEQUENTIAL.
                       SELECT F2 ASSIGN TO "irr_f2.dat" ORGANIZATION IS SEQUENTIAL.
                   I-O-CONTROL.
                       SAME RECORD AREA FOR F1 F2.
                   DATA DIVISION.
                   FILE SECTION.
                   FD F1.
                   01 R1 PIC X(4).
                   FD F2.
                   01 R2 TYPE T1.
                   WORKING-STORAGE SECTION.
            {Strong}
                   PROCEDURE DIVISION.
                       STOP RUN.
            """;
        var (d, ed) = Compile(src);
        Assert.Equal(RedefinitionKind.SameRecordArea, d.ByName["R2"][0].RedefinesKind);
        var sr4 = Assert.Single(ed.Diagnostics, x => x.Contains("COBOLNET1532", StringComparison.Ordinal));
        Assert.Contains("§12.4.6.4.4 GR2", sr4);
        Assert.DoesNotContain(ed.Diagnostics, x => x.Contains("§13.18.44", StringComparison.Ordinal));
    }

    /// <summary>A level-1 <c>USAGE POINTER</c> record is legal (§13.18.60.3 SR14 admits a pointer at level 1) and a
    /// second record shares its area by §13.18.33.4 GR3 — no REDEFINES clause, so no SR12. The shared backing has no
    /// managed slot to carry the pointer, which is the storage model's limit: staged loud as COBOLNET0899, citing the
    /// rule the program used.</summary>
    [Fact]
    public void PointerRecord_InAMultiRecordFd_IsStagedLoud_NotSr12()
    {
        var (d, ed) = Compile(Fd("01 R2 PIC X(10).\n       01 R3 USAGE POINTER.", ""));
        Assert.Equal(RedefinitionKind.ImplicitFileRecord, d.ByName["R3"][0].RedefinesKind);
        Assert.Equal(RedefinesTier.Rejected, d.ByName["R3"][0].Class!.Tier);
        Assert.DoesNotContain(ed.Diagnostics, x => x.Contains("COBOLNET1697", StringComparison.Ordinal));
        var staged = Assert.Single(ed.Diagnostics, x => x.Contains("COBOLNET0899", StringComparison.Ordinal));
        Assert.Contains("§13.18.33.4 GR3", staged);
        Assert.Contains("in the shared record area of 'R2'", staged);
        Assert.DoesNotContain("REDEFINES", staged);
    }

    /// <summary>The record kinds, and the clause kind beside them: the one fact every clause screen now reads.</summary>
    [Fact]
    public void TheKind_NamesTheConstruct()
    {
        var (d, ed) = Compile(Fd("01 R1 PIC X(4).\n       01 R2 PIC 9(4).",
            "       01 W1 PIC X(4).\n       01 W2 REDEFINES W1 PIC 9(4)."));
        Assert.Empty(ed.Diagnostics);
        Assert.Equal(RedefinitionKind.None, d.ByName["R1"][0].RedefinesKind);
        Assert.Equal(RedefinitionKind.ImplicitFileRecord, d.ByName["R2"][0].RedefinesKind);
        Assert.Same(d.ByName["R1"][0], d.ByName["R2"][0].RedefinesTarget);
        Assert.Equal(RedefinitionKind.Clause, d.ByName["W2"][0].RedefinesKind);
        // One storage fact for both constructs: both pairs are ONE class.
        Assert.Same(d.ByName["R1"][0].Class, d.ByName["R2"][0].Class);
        Assert.Same(d.ByName["W1"][0].Class, d.ByName["W2"][0].Class);
    }

    private static string Fd(string records, string ws) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. IRRFD.
               ENVIRONMENT DIVISION.
               INPUT-OUTPUT SECTION.
               FILE-CONTROL.
                   SELECT F ASSIGN TO "irr_f.dat" ORGANIZATION IS SEQUENTIAL.
               DATA DIVISION.
               FILE SECTION.
               FD F.
               {records}
               WORKING-STORAGE SECTION.
        {ws}
               PROCEDURE DIVISION.
                   STOP RUN.
        """;

    /// <summary>Bind a whole program through <c>BinderDriver.Bind</c> (the group tail included) at COBOL-2023.</summary>
    private static (DataBinder Data, EditionContext Edition) Compile(string src)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_irr_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, src);
        try
        {
            var diags = new DiagnosticBag();
            var tree = new CnFrontend { DialectLevel = 2023 }.Parse(path, diags);
            Assert.False(diags.HasErrors, string.Join("\n", diags.Diagnostics));
            Assert.NotNull(tree);
            var ed = new EditionContext(2023);
            var bound = new BinderDriver().Bind(tree!, ed);
            return (bound.Units[0].Data, ed);
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }
}
