// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// NIST programs whose original GOLDEN was legacy-tainted — the baselined expected file encoded a LEGACY
/// non-conformance. Each pin asserts the SPEC-derived outcome with its ISO citation. The goldens below were
/// RE-BASELINED to the conforming output (owner-approved, DEVLOG 569; <c>tests/nist/corpus.tsv</c> marks each row
/// <c>divergent</c> with its ISO citation), so the programs are ALSO byte-locked in <see cref="NistDifferentialTests"/>;
/// these pins remain as the citation-bearing documentation of WHY each golden diverges from the legacy.
/// <para>ONE pin is of a different KIND and is marked as such: <see cref="NC201A_VaryingAfterFromOuterInductionVariable_At1985_RunsSixBodies"/>
/// pins a PER-EDITION ORDER, not a legacy artefact: NC201A is a COBOL-85 program and the 1985 text orders the two
/// operations of an AFTER step the way its CCVS check asserts (kb/Work PB5101).</para>
/// </summary>
public sealed class SpecPinnedNistTests
{
    /// <summary>NC236A: the golden marks SCH-TEST-F1-8 / F1-10 "TEST DELETED" — a legacy ARTIFACT: the legacy's
    /// serial SEARCH with <c>VARYING index-of-ANOTHER-table</c> (ISO §2023:14.9.37.4 GR3 c) 2.) falls through to the CCVS
    /// DE-LETE paragraph instead of executing the scan (verified by running the legacy directly, DEVLOG 533).
    /// Per GR3 c) 2. the scan uses the searched table's own first index and varies the other index in step — both
    /// tests then PASS: the conforming run executes 010 OF 010 with nothing deleted.</summary>
    [Fact]
    public void NC236A_SearchVaryingOtherTableIndex_ExecutesAllTests()
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Pin_");
        try
        {
            var r = CompiledProgramCache.Compile(CorpusManifest.CompileOptions("NC236A", Path.Combine(dir, "NC236A.dll"), 85, permissive: false, checkOnly: false));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC236A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc236a.txt"));

            Assert.Contains("PASS  SCH-TEST-F1-8", output);
            Assert.Contains("PASS  SCH-TEST-F1-10", output);
            Assert.Contains("010 OF 010  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>NC235A: the golden marks IDX-TEST-F2-9 / F2-12 "TEST DELETED" — a legacy artifact: its SEARCH ALL
    /// over an OCCURS-DEPENDING table with a CONDITION-NAME WHEN (<c>WHEN LASTA (IDX-1)</c>) fell through to the
    /// CCVS DE-LETE paragraph. Per ISO §2023:14.9.37 Format 2 (a WHEN condition-name over a table element keyed by the
    /// search index) + §2023:13.18.38 GR7 (the table's count IS data-name-1's value) both tests execute and PASS — the
    /// conforming run is 013 OF 013 with nothing deleted.</summary>
    [Fact]
    public void NC235A_SearchAllConditionNameOverOdo_ExecutesAllTests()
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Pin_");
        try
        {
            var r = CompiledProgramCache.Compile(CorpusManifest.CompileOptions("NC235A", Path.Combine(dir, "NC235A.dll"), 85, permissive: false, checkOnly: false));
            Assert.True(r.Success, string.Join("; ", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC235A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc235a.txt"));

            Assert.Contains("PASS  IDX-TEST-F2-9", output);
            Assert.Contains("PASS  IDX-TEST-F2-12", output);
            Assert.Contains("013 OF 013  TESTS WERE EXECUTED SUCCESSFULLY", output);
            Assert.DoesNotContain("TEST DELETED", output);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>ST127A at <c>--std 2002</c> — the FREE, EXTERNALLY-AUTHORED witness for kb/Work PB704, and the
    /// one the NIST leg structurally cannot give. <c>ST127A.cob:257</c> writes <c>WITH DUPLICATES IN ORDER</c>
    /// (ISO §2023:14.9.40.2), the only occurrence of the phrase anywhere in the tree, and it passed for two trains
    /// only because the NIST leg runs at <c>--std 85</c>, where §2023:8.9 does not yet reserve ORDER: the leg is
    /// evidence about COBOL-85 and about nothing else (feedback_measure_the_selectors_complement). While the
    /// phrase's own word rode <c>cobolWord</c>, the §2023:8.9 funnel — which screens IDENTIFIER occurrences
    /// POSITION-BLIND — refused it as a user-defined word from 2002 on, so a real CCVS program was rejected at
    /// every edition a user is likely to target.
    /// <para>⚠ THE ASSERTION IS THE ABSENCE OF COBOL0901, NOT A CLEAN COMPILE, and deliberately so. ST127A is
    /// an X3.23-1985 program: at strict COBOL-2002 its SD still carries the DATA RECORDS clause, which ISO 2002
    /// DELETED (COBOL0873), so a "compiles clean" assertion could only be made under <c>--permissive</c> —
    /// and under <c>--permissive</c> the reservation gate does not fire and the 0901 degrades to a warning, so
    /// that spelling of the test would have passed BEFORE the fix as well (feedback_green_gates_arent_evidence).
    /// The strict compile with 0901 excluded fails on the old compiler and passes on the new one.</para></summary>
    [Fact]
    public void ST127A_SortDuplicatesInOrder_IsNotAReservedWordViolationAt2002()
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Pin_");
        try
        {
            var r = CompiledProgramCache.Compile(CorpusManifest.CompileOptions("ST127A", Path.Combine(dir, "ST127A.dll"), 2002, permissive: false, checkOnly: false));
            string all = string.Join("\n", r.Errors);
            Assert.DoesNotContain("COBOL0901", all, StringComparison.Ordinal);
            Assert.DoesNotContain("'ORDER'", all, StringComparison.Ordinal);
            // The program's ONE remaining strict-2002 obstacle is the deleted FD/SD clause, not the SORT phrase —
            // asserted so a future change that silently stops compiling the SORT at all cannot pass this test.
            Assert.Contains("COBOL0873", all, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>NC201A PFM-TEST-F4-23 at <c>--std 85</c> (kb/Work PB5101; before it, PB436 recorded the check as a CCVS
    /// defect and the golden carried the FAIL).
    /// <para>The CCVS check, titled "ORDER OF INITIALISATION OF VARYING IDENTIFIERS" and referenced to the 1985
    /// text as <c>VI-114 6.20.4 GR10(d)1</c>, executes
    /// <c>PERFORM PFM-F4-23-PROC VARYING PFM-A1 FROM 1 BY 1 UNTIL PFM-A1 &gt; 3 AFTER PFM-B1 FROM PFM-A1 BY 1
    /// UNTIL PFM-B1 &gt; 3</c> and asserts the body ran SIX times — i.e. that PFM-A1 is augmented BEFORE PFM-B1
    /// is reset from it (3+2+1). §1985:VI.6.21.4 GR10 d) 1 orders the two that way: "the content of the data item
    /// referenced by identifier-2 is augmented …" and then "the content of the data item referenced by identifier-5
    /// is set to literal-3 or the current value of the data item referenced by identifier-6", so the body runs SIX
    /// times at <c>--std 85</c>. From 2002 §2023:14.9.28.4 GR13 e) 2 a–c reset the inner variable first and the body
    /// runs EIGHT times (witness <c>conformance:cases/14.9.28-PERFORM/pb436_varying_after_from_outer</c>); the
    /// determination is in <c>docs/CONFORMANCE.md</c> §3.</para>
    /// <para>⚠ THE ASSERTION IS THE PASS LINE AND THE FOOTER, NOT ONLY THE GOLDEN: <c>NistDifferentialTests</c> masks
    /// <c>COMPUTED=</c> operands on both sides of its byte comparison, so this pin reads the report's own verdict.</para></summary>
    [Fact]
    public void NC201A_VaryingAfterFromOuterInductionVariable_At1985_RunsSixBodies()
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_Pin_");
        try
        {
            var r = CompiledProgramCache.Compile(CorpusManifest.CompileOptions("NC201A", Path.Combine(dir, "NC201A.dll"), 85, permissive: false, checkOnly: false));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            var (runOk, _, detail) = CutRunner.Run(Path.Combine(dir, "NC201A.dll"), dir);
            Assert.True(runOk, detail);
            string output = File.ReadAllText(Path.Combine(dir, "nc201a.txt"));

            Assert.Contains("PASS  PFM-TEST-F4-23", output, StringComparison.Ordinal);
            Assert.DoesNotContain("FAIL*", output, StringComparison.Ordinal);
            Assert.Contains("059 OF 059  TESTS WERE EXECUTED SUCCESSFULLY", output, StringComparison.Ordinal);
            Assert.Contains("NO  TEST(S) FAILED", output, StringComparison.Ordinal);
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
