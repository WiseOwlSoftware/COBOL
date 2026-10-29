// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using WiseOwl.Cobol;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ AN ARITHMETIC STATEMENT EVALUATES ITS RIGHT-HAND SIDE ONCE, AND §14.7.4.3 r7 IS ASKED BY THE LANDING OF THAT ONE
/// INTERMEDIATE (kb/Work PB654, PB3990). <c>ArithmeticEmitter.StoreArith</c> wrote <c>value.Expr</c> into the
/// PROHIBITED representability test and AGAIN into the store that follows. ISO §14.7.7 rule 4 a) — "The
/// initial evaluation of the statement is done and the result of this operation is placed in an intermediate
/// data item" — and rule 4 b) — "the intermediate data item is stored in or combined with and then stored in
/// each single resulting data item" — admit exactly one evaluation, and rule 7's test is a test on THAT
/// intermediate.
/// <para>The cost was a wrong answer, not merely a wasted multiply: for a function whose successive references
/// are not the same value (§15.75.3 rule 5 — "In each case, subsequent references without specifying
/// argument-1 return the next number in the current sequence") the test examined one returned value and the
/// store landed the NEXT one, and the statement consumed two elements of the pseudo-random sequence.</para>
/// <para>This class reads the EMITTED C# because that is where the defect lives: the corpus golden
/// <c>conformance-suite/cases/_unassigned/pb654_prohibited_gate_one_evaluation</c> pins the BEHAVIOUR through RANDOM's
/// sequence position, and these pin the SHAPE over every receiver category and phrase combination, so a new
/// arm that re-spells the value fails here even if no golden happens to cross it.</para>
/// </summary>
public sealed class ArithmeticOneInitialEvaluationDriftTests
{
    /// <summary>Six arithmetic stores, one <c>FUNCTION RANDOM</c> reference each, across every receiver
    /// category and phrase combination that reaches <c>StoreArith</c> with a float value: plain numeric and
    /// numeric-EDITED under ON SIZE ERROR + PROHIBITED, a non-PROHIBITED mode under ON SIZE ERROR, PROHIBITED with no
    /// phrase at all (rule 7 keeps the receiver there too, kb/Work PB3990: the one rule-7 catch), a bare no-phrase
    /// store, and a FLOAT receiver (no scaled store at all).</summary>
    private const string Program = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. PB654DRIFT.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 R   PIC 9V9(9).
        01 ED  PIC ZZ9.999.
        01 C2  USAGE COMP-2.
        PROCEDURE DIVISION.
        MAIN.
            COMPUTE C2 = 1.
            COMPUTE R ROUNDED MODE IS PROHIBITED = C2 * FUNCTION RANDOM
                ON SIZE ERROR CONTINUE
            END-COMPUTE.
            COMPUTE ED ROUNDED MODE IS PROHIBITED = C2 * FUNCTION RANDOM
                ON SIZE ERROR CONTINUE
            END-COMPUTE.
            COMPUTE R ROUNDED MODE IS NEAREST-EVEN = C2 * FUNCTION RANDOM
                ON SIZE ERROR CONTINUE
            END-COMPUTE.
            COMPUTE R ROUNDED MODE IS PROHIBITED = C2 * FUNCTION RANDOM.
            COMPUTE R = C2 * FUNCTION RANDOM.
            COMPUTE C2 = C2 * FUNCTION RANDOM.
            STOP RUN.
        """;

    /// <summary>The number of <c>FUNCTION RANDOM</c> references written in <see cref="Program"/>.</summary>
    private const int SourceReferences = 6;

    private static string EmitCSharp(string source, int edition)
    {
        string dir = ScratchDirectory.Create("pb654drift");
        try
        {
            string src = Path.Combine(dir, "p.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "p.dll"), DialectLevel: edition, CheckOnly: false, SourceFormat: InitialReferenceFormat.Auto));
            Assert.True(r.Success, "the drift program must compile: " + string.Join("; ", r.Errors));
            Assert.NotNull(r.GeneratedCsPath);
            return File.ReadAllText(r.GeneratedCsPath!);
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>
    /// INVARIANT 1 — THE EMITTED C# SPELLS EACH RIGHT-HAND SIDE EXACTLY AS OFTEN AS THE SOURCE WROTE IT.
    /// A second spelling IS a second evaluation (§14.7.7 rule 4 a)), and for <c>FUNCTION RANDOM</c> a second
    /// evaluation is a different value. Counting the CALL TEXT rather than reasoning about the branch shape is
    /// deliberate: it catches the defect wherever a future arm reintroduces it.
    /// </summary>
    [Fact]
    public void EachRightHandSide_IsSpelledOncePerSourceReference()
    {
        string cs = EmitCSharp(Program, 2014);
        int emitted = Regex.Matches(cs, @"CobolIntrinsics\.Random\(\)").Count;
        Assert.Equal(SourceReferences, emitted);
    }

    /// <summary>
    /// INVARIANT 2 — NO SEPARATE EXACTNESS TEST IS WRITTEN BESIDE THE LANDING (kb/Work PB3990, replacing PB654's gate).
    /// §14.7.4.3 rule 7 is answered by the landing's own rounding decision on every receiver category, checked or not
    /// (<c>CobolSizeError.ProhibitedInexact</c>), so the value is read once by construction. A test written beside the
    /// store is a second read of the intermediate (invariant 1's defect) and a second rule that can disagree with the
    /// landing: PB654's gate covered the checked statement only, and the unchecked one stored the value cut.
    /// </summary>
    [Fact]
    public void NoProhibitedGate_IsWrittenBesideTheLanding()
    {
        string cs = EmitCSharp(Program, 2014);
        Assert.DoesNotMatch(@"InexactAtScale\(", cs);
    }

    /// <summary>
    /// INVARIANT 3 — THE STATEMENT WITH NO SIZE ERROR PHRASE AND CHECKING OFF KEEPS A PROHIBITED RECEIVER ON RULE 7'S
    /// CONDITION ALONE. <see cref="Program"/> writes ONE such statement (the fourth store), so the emitted C# catches
    /// <c>CobolSizeError</c> filtered on <c>IsProhibitedInexact</c> exactly once: the other PROHIBITED stores have their
    /// statement's own ON SIZE ERROR catch, and no other mode pays for a try.
    /// </summary>
    [Fact]
    public void TheUncheckedProhibitedStore_CatchesRule7Alone_Once()
    {
        string cs = EmitCSharp(Program, 2014);
        Assert.Single(Regex.Matches(cs, @"catch \(CobolSizeError (__cse\d+)\) when \(\1\.IsProhibitedInexact\) \{ \}"));
    }
}
