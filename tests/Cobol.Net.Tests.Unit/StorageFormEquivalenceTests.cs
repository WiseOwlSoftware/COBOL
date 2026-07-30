// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Binding.Passes;
using CobolNet.CodeGen;
using CobolNet.Frontend.Diagnostics;
using CobolNet.Tests.Shared;
using Xunit;
using CnFrontend = CobolNet.Frontend.Frontend;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The PHASE-05 Step 2 (D0) prove-then-delete gate: the new <see cref="StorageFormPass"/> computes a canonical
/// <see cref="Model.StorageForm"/> in parallel with the legacy <c>StoreAsImage</c> flag + the recursive image-fact
/// properties, and this test asserts they are EQUAL corpus-wide (four identities per item — see
/// <see cref="StorageFormPass.Verify"/>) BEFORE any deletion. Drives the FULL <c>emitter.Bind</c> path (which runs
/// <c>MarkStoreAsImage</c> + the OO harmonize + <c>StorageFormPass.Compute</c>) — NOT the bare <c>DataBinder.Bind</c>,
/// which skips the middle-end passes. (Exit criterion #3.)
/// </summary>
public sealed class StorageFormEquivalenceTests
{
    /// <summary>Bind <paramref name="source"/> via the full pipeline and assert zero StorageForm divergences.</summary>
    private static void VerifySource(string source, int dialect)
    {
        string path = Path.Combine(Path.GetTempPath(), "cn_sf_" + Guid.NewGuid().ToString("N")[..8] + ".cob");
        File.WriteAllText(path, source);
        try
        {
            var (divergences, ok) = Analyze(path, dialect);
            Assert.True(ok, $"frontend failed to parse the crafted source");
            Assert.True(divergences.Count == 0,
                $"{divergences.Count} StorageForm divergence(s):\n" + string.Join("\n", divergences.Take(10)));
        }
        finally { try { File.Delete(path); } catch { /* best-effort */ } }
    }

    /// <summary>Bind one .cob file and return (divergences, parsed-ok). A program that fails the FRONTEND (parse /
    /// missing COPY) is skipped (parsed-ok=false) — it exercises no StorageForm.</summary>
    private static (List<string> Divergences, bool Ok) Analyze(string path, int dialect)
    {
        var diags = new DiagnosticBag();
        var frontend = new CnFrontend { DialectLevel = dialect };
        // Resolve the NIST COPY library the SM/COPY suite needs (sibling copylib/, the CLI convention).
        if (Path.GetDirectoryName(Path.GetFullPath(path)) is { } srcDir
            && Path.GetFullPath(Path.Combine(srcDir, "..", "copylib")) is { } copylib && Directory.Exists(copylib))
            frontend.AddCopySearchPath(copylib);
        var tree = frontend.Parse(path, diags);
        if (tree is null || diags.HasErrors) return ([], false);

        var emitter = new CSharpEmitter();
        var edition = new EditionContext(dialect, permissive: false);
        var bound = emitter.Bind(tree, edition, frontend.TurnEvents);   // runs MarkStoreAsImage + OO harmonize + StorageFormPass.Compute

        var binders = bound.Units.Select(u => u.Data)
            .Concat(bound.ClassUnits.SelectMany(c => new[] { c.Data, c.FactoryData }));
        var divergences = new List<string>();
        foreach (var b in binders)
        {
            divergences.AddRange(StorageFormPass.Verify(b));
        }
        return (divergences, true);
    }

    [Theory]
    // A whole-group MOVE promotes the group's numeric-DISPLAY leaf to its string image (§14.9 GR4) — Storage must be
    // CharImage(Numeric) (ElementType "string"), exactly as the legacy StoreAsImage flag makes it.
    [InlineData(WholeGroupMove)]
    // A fixed-OCCURS numeric-DISPLAY element under a whole group is promoted per occurrence (element type string[]).
    [InlineData(FixedOccursUnderWholeGroup)]
    // SIGN IS SEPARATE adds a character position — the amended NativeInt.Width (digits + 1) must reproduce ImageWidth.
    [InlineData(SignSeparate)]
    // Mixed native usages NOT under a whole group stay native (NativeInt/NativeFloat), a group summing their widths.
    [InlineData(MixedNativeUsages)]
    // A REDEFINES view (Tier classification → TierBWindow / CharImage) — parity with StoreAsImage on the members.
    [InlineData(RedefinesView)]
    public void CraftedCases(string source) => VerifySource(source, 2002);

    [Fact]
    public void NistCorpus_StorageFormEqualsLegacy()
    {
        string dir = TestRepo.Nist("programs");
        Assert.True(Directory.Exists(dir), $"NIST corpus dir not found: {dir}");
        var progs = Directory.GetFiles(dir, "*.cob").OrderBy(p => p).ToList();
        Assert.NotEmpty(progs);

        int parsed = 0;
        var allDivergences = new List<string>();
        foreach (var p in progs)
        {
            var (divergences, ok) = Analyze(p, 85);
            if (!ok) continue;
            parsed++;
            allDivergences.AddRange(divergences.Select(d => $"{Path.GetFileName(p)}: {d}"));
        }
        Assert.True(parsed >= 50, $"expected ≥50 NIST programs to parse, got {parsed}");
        Assert.True(allDivergences.Count == 0,
            $"{allDivergences.Count} StorageForm divergence(s) across {parsed} NIST programs:\n"
            + string.Join("\n", allDivergences.Take(15)));
    }

    private const string WholeGroupMove = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. WGM.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 GRP.
           05 A PIC 9(4).
           05 B PIC X(3).
        01 DEST PIC X(7).
        PROCEDURE DIVISION.
        MAIN.
            MOVE GRP TO DEST.
            STOP RUN.
        """;

    private const string FixedOccursUnderWholeGroup = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. FOW.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 GRP.
           05 T PIC 9(2) OCCURS 3 TIMES.
        01 DEST PIC X(6).
        PROCEDURE DIVISION.
        MAIN.
            MOVE GRP TO DEST.
            STOP RUN.
        """;

    private const string SignSeparate = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. SGS.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 GRP.
           05 S PIC S9(4) SIGN LEADING SEPARATE.
           05 F PIC X(2).
        01 DEST PIC X(7).
        PROCEDURE DIVISION.
        MAIN.
            MOVE GRP TO DEST.
            STOP RUN.
        """;

    private const string MixedNativeUsages = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. MNU.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS.
           05 N1 PIC 9(4).
           05 N2 PIC 9(6) COMP.
           05 N3 PIC 9(8) COMP-3.
           05 N4 COMP-1.
           05 N5 PIC S9(18).
           05 A1 PIC X(5).
        PROCEDURE DIVISION.
        MAIN.
            DISPLAY N1 N2 N3 N4 N5 A1.
            STOP RUN.
        """;

    private const string RedefinesView = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. RDV.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 REC.
           05 PART-A PIC X(6).
           05 PART-B REDEFINES PART-A.
              10 P1 PIC 9(3).
              10 P2 PIC X(3).
        PROCEDURE DIVISION.
        MAIN.
            MOVE "ABCDEF" TO PART-A.
            DISPLAY P1.
            STOP RUN.
        """;
}
