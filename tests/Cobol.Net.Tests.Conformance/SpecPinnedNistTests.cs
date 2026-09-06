// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// NIST programs whose original GOLDEN was legacy-tainted — the baselined expected file encoded a LEGACY
/// non-conformance. Each pin asserts the SPEC-derived outcome with its ISO citation. The goldens below were
/// RE-BASELINED to the conforming output (owner-approved, DEVLOG 569 — the legacy guard carries them in its
/// LEGACY_DIVERGENT list), so the programs are ALSO byte-locked in <see cref="NistDifferentialTests"/>;
/// these pins remain as the citation-bearing documentation of WHY each golden diverges from the legacy.
/// </summary>
public sealed class SpecPinnedNistTests
{
    /// <summary>NC236A: the golden marks SCH-TEST-F1-8 / F1-10 "TEST DELETED" — a legacy ARTIFACT: the legacy's
    /// serial SEARCH with <c>VARYING index-of-ANOTHER-table</c> (ISO §14.9.37.4 GR8b) falls through to the CCVS
    /// DE-LETE paragraph instead of executing the scan (verified by running the legacy directly, DEVLOG 533).
    /// Per GR8b the scan uses the searched table's own first index and varies the other index in step — both
    /// tests then PASS: the conforming run executes 010 OF 010 with nothing deleted.</summary>
    [Fact]
    public void NC236A_SearchVaryingOtherTableIndex_ExecutesAllTests()
    {
        string src = TestRepo.Nist("programs", "NC236A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "NC236A.dll"), NistTestName: "NC236A", DialectLevel: 85));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC236A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc236a.txt"));

            Assert.Contains("PASS  SCH-TEST-F1-8", output);
            Assert.Contains("PASS  SCH-TEST-F1-10", output);
            Assert.Contains("010 OF 010  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>NC235A: the golden marks IDX-TEST-F2-9 / F2-12 "TEST DELETED" — a legacy artifact: its SEARCH ALL
    /// over an OCCURS-DEPENDING table with a CONDITION-NAME WHEN (<c>WHEN LASTA (IDX-1)</c>) fell through to the
    /// CCVS DE-LETE paragraph. Per ISO §14.9.37 Format 2 (a WHEN condition-name over a table element keyed by the
    /// search index) + §13.18.38 GR7 (the table's count IS data-name-1's value) both tests execute and PASS — the
    /// conforming run is 013 OF 013 with nothing deleted.</summary>
    [Fact]
    public void NC235A_SearchAllConditionNameOverOdo_ExecutesAllTests()
    {
        string src = TestRepo.Nist("programs", "NC235A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "NC235A.dll"), NistTestName: "NC235A", DialectLevel: 85));
            Assert.True(r.Success, string.Join("; ", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC235A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc235a.txt"));

            Assert.Contains("PASS  IDX-TEST-F2-9", output);
            Assert.Contains("PASS  IDX-TEST-F2-12", output);
            Assert.Contains("013 OF 013  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { CutRunner.TryDelete(dir); }
    }

    /// <summary>ST127A at <c>--std 2002</c> — the FREE, EXTERNALLY-AUTHORED witness for kb/Work PB704, and the
    /// one the NIST leg structurally cannot give. <c>ST127A.cob:257</c> writes <c>WITH DUPLICATES IN ORDER</c>
    /// (ISO §14.9.40.2), the only occurrence of the phrase anywhere in the tree, and it passed for two trains
    /// only because the NIST leg runs at <c>--std 85</c>, where §8.9 does not yet reserve ORDER: the leg is
    /// evidence about COBOL-85 and about nothing else (feedback_measure_the_selectors_complement). While the
    /// phrase's own word rode <c>cobolWord</c>, the §8.9 funnel — which screens IDENTIFIER occurrences
    /// POSITION-BLIND — refused it as a user-defined word from 2002 on, so a real CCVS program was rejected at
    /// every edition a user is likely to target.
    /// <para>⚠ THE ASSERTION IS THE ABSENCE OF COBOLNET0901, NOT A CLEAN COMPILE, and deliberately so. ST127A is
    /// an X3.23-1985 program: at strict COBOL-2002 its SD still carries the DATA RECORDS clause, which ISO 2002
    /// DELETED (COBOLNET0873), so a "compiles clean" assertion could only be made under <c>--permissive</c> —
    /// and under <c>--permissive</c> the reservation gate does not fire and the 0901 degrades to a warning, so
    /// that spelling of the test would have passed BEFORE the fix as well (feedback_green_gates_arent_evidence).
    /// The strict compile with 0901 excluded fails on the old compiler and passes on the new one.</para></summary>
    [Fact]
    public void ST127A_SortDuplicatesInOrder_IsNotAReservedWordViolationAt2002()
    {
        string src = TestRepo.Nist("programs", "ST127A.cob");
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_Pin_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "ST127A.dll"), NistTestName: "ST127A", DialectLevel: 2002));
            string all = string.Join("\n", r.Errors);
            Assert.DoesNotContain("COBOLNET0901", all, StringComparison.Ordinal);
            Assert.DoesNotContain("'ORDER'", all, StringComparison.Ordinal);
            // The program's ONE remaining strict-2002 obstacle is the deleted FD/SD clause, not the SORT phrase —
            // asserted so a future change that silently stops compiling the SORT at all cannot pass this test.
            Assert.Contains("COBOLNET0873", all, StringComparison.Ordinal);
        }
        finally { CutRunner.TryDelete(dir); }
    }
}
