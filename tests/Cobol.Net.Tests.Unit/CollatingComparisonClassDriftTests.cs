// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ TWO QUESTIONS, TWO CLASSIFIERS — the guard that keeps them from being folded onto one again (kb/Work PB741).
///
/// <para><b>What happened.</b> <c>CollatingSelection.Of(PicCategory?)</c> answers a ONE-OPERAND question: what
/// class is this SORT/MERGE KEY, and which of §14.9.40.4 GR5 / §14.9.24.4 GR5's two separately-determined
/// sequences does it take? A key of class numeric takes NEITHER. kb/Work PB678 (<c>f798397f</c>) then routed the
/// RELATION-CONDITION renderer's collating argument through that same classifier — but a relation asks a
/// TWO-OPERAND question, and §8.8.4.2.5 makes a numeric integer operand compared with an ALPHANUMERIC operand an
/// ALPHANUMERIC comparison, which §8.8.4.2.7 collates under the alphanumeric program collating sequence. Asking
/// only the numeric side returned "no sequence", so <c>IF NINE-DU-9V0-1 &lt; SPACE</c> silently ignored
/// <c>PROGRAM COLLATING SEQUENCE</c> and NIST NC215A's SEQ-TEST-GF-6/-7 went PASS → FAIL*.</para>
///
/// <para><b>What this pins.</b> (1) the comparison-class matrix, as literal data rather than re-derived code, so a
/// future edit to any arm is deliberate; (2) the exact pair where the two classifiers MUST disagree — the fold
/// that caused PB741 cannot come back without turning this red; (3) that the reference categories §8.8.4.2.14/.15
/// /.16 govern are answered by identity BEFORE any category is classified, which is why the comparison-class rule
/// has no arm for them; (4) the end-to-end answer, over the NC215A shape, that a numeric-DISPLAY operand compared
/// with a figurative constant and with the equivalent alphanumeric literal give the SAME answer under a declared
/// program collating sequence — the internal inconsistency PB741 exposed (measured FALSE vs TRUE before the fix).
/// </para>
/// </summary>
public sealed class CollatingComparisonClassDriftTests : CobolNetTestBase
{
    /// <summary>The operand categories a relation condition's comparison-class rule can be asked about. The
    /// reference categories (object, pointer, program-pointer) are deliberately absent — see
    /// <see cref="ReferenceRelations_AreAnsweredByIdentity_BeforeAnyCategoryIsClassified"/>.</summary>
    private static readonly PicCategory?[] Axis =
    [
        null,                        // an ordinary alphanumeric GROUP, or a category-less operand
        PicCategory.Group,
        PicCategory.Alphanumeric,    // PIC X and PIC A both land here (§8.8.4.2.1 — alphabetic is "treated as" alphanumeric)
        PicCategory.NumericEdited,
        PicCategory.Numeric,
        PicCategory.National,
        PicCategory.Boolean,
    ];

    /// <summary>The expected class per (row = left, column = right) pair, in <see cref="Axis"/> order.
    /// <c>A</c> = alphanumeric (§8.8.4.2.7), <c>N</c> = national (§8.8.4.2.9), <c>B</c> = boolean (§8.8.4.2.8),
    /// <c>#</c> = numeric (§8.8.4.2.4). ⛔ The ONE <c>#</c> is the whole rule: only BOTH operands numeric is an
    /// algebraic, sequence-free comparison. Every other cell in the Numeric row and column is <c>A</c> or <c>N</c>
    /// because §8.8.4.2.5 moves the numeric integer operand to "the same class and usage as the alphanumeric or
    /// national operand" and compares by THAT class's rules.</summary>
    private static readonly string[] Matrix =
    [
        //        null Grp  Alph NumE Num  Natl Bool
        /* null */ "A   A    A    A    A    N    B",
        /* Grp  */ "A   A    A    A    A    N    B",
        /* Alph */ "A   A    A    A    A    N    B",
        /* NumE */ "A   A    A    A    A    N    B",
        /* Num  */ "A   A    A    A    #    N    B",
        /* Natl */ "N   N    N    N    N    N    B",
        /* Bool */ "B   B    B    B    B    B    B",
    ];

    private static CollatingClass Expected(char c) => c switch
    {
        'A' => CollatingClass.Alphanumeric,
        'N' => CollatingClass.National,
        'B' => CollatingClass.Boolean,
        '#' => CollatingClass.Numeric,
        _ => throw new InvalidOperationException($"bad matrix cell '{c}'"),
    };

    [Fact]
    public void ForComparison_AnswersTheWholeCategoryMatrix_AndIsSymmetric()
    {
        Assert.Equal(Axis.Length, Matrix.Length);
        for (int i = 0; i < Axis.Length; i++)
        {
            var cells = Matrix[i].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(Axis.Length, cells.Length);
            for (int j = 0; j < Axis.Length; j++)
            {
                var want = Expected(cells[j][0]);
                Assert.Equal(want, CollatingSelection.ForComparison(Axis[i], Axis[j]));
                // A relation's comparison rule does not depend on which operand is the subject (§8.8.4.2.1 names
                // the subject and object only for the RELATIONAL OPERATOR's direction), so the rule is symmetric.
                Assert.Equal(want, CollatingSelection.ForComparison(Axis[j], Axis[i]));
            }
        }
    }

    /// <summary>⛔ THE PB741 ASSERTION. The two classifiers answer the same category differently, on purpose: a
    /// numeric SORT KEY takes no sequence (§14.9.40.4 GR5 names one for class alphabetic/alphanumeric and one for
    /// class national, and none for class numeric), while a numeric operand COMPARED with an alphanumeric one is an
    /// alphanumeric comparison that §8.8.4.2.7 collates. Folding the second onto the first is the regression.</summary>
    [Fact]
    public void TheSortKeyClassifier_AndTheComparisonClassifier_DisagreeOnANumericOperand()
    {
        Assert.Equal(CollatingClass.Numeric, CollatingSelection.Of(PicCategory.Numeric));
        Assert.Equal(CollatingClass.Alphanumeric,
            CollatingSelection.ForComparison(PicCategory.Numeric, PicCategory.Alphanumeric));
        Assert.Equal(CollatingClass.National,
            CollatingSelection.ForComparison(PicCategory.Numeric, PicCategory.National));
        // …and they still AGREE wherever the one-operand answer is the whole answer, so the split is not a fork.
        foreach (var c in new PicCategory?[] { null, PicCategory.Alphanumeric, PicCategory.National, PicCategory.Boolean })
            Assert.Equal(CollatingSelection.Of(c), CollatingSelection.ForComparison(c, c));
    }

    /// <summary>The comparison-class rule has no arm for the reference categories because a message-tag, object or
    /// pointer relation is a reference-IDENTITY comparison (§8.8.4.2.14/.15/.16) that the relation renderer answers
    /// and returns from BEFORE it classifies anything. This reads the renderer's source order, because the property
    /// being guarded is exactly that ordering — a behavioural probe would pass on a renderer that classified first
    /// and happened not to use the answer.</summary>
    [Fact]
    public void ReferenceRelations_AreAnsweredByIdentity_BeforeAnyCategoryIsClassified()
    {
        // CODE ONLY — the comments name these symbols too, and an index into the raw text would measure prose
        // order rather than execution order (this test caught exactly that on its first run).
        string src = string.Join('\n', File.ReadAllLines(Path.Combine(TestRepo.Root,
                "src", "Cobol.Net.Compiler", "CodeGen", "Emit", "ConditionRenderer.cs"))
            .Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
        int classify = src.IndexOf("CollatingSelection.ForComparison", StringComparison.Ordinal);
        Assert.True(classify > 0, "the relation renderer no longer calls the comparison-class rule");
        foreach (var identity in new[] { "object.ReferenceEquals(", "ManagedPointer.SameTarget(", "ProgramPointer.SameTarget(" })
        {
            int at = src.IndexOf(identity, StringComparison.Ordinal);
            Assert.True(at > 0, $"the identity relation '{identity}' is gone from the relation renderer");
            Assert.True(at < classify,
                $"'{identity}' must be answered BEFORE the comparison class is asked — §8.8.4.2.14/.15/.16 "
                + "relations have no collating sequence, and the class rule has no arm for their categories");
        }
    }

    /// <summary>⛔ THE CATEGORY READER MAY NOT REGROW (kb/Work PB728). <c>DataItem.Pic</c> is null for EVERY group,
    /// so reading a category off it silently reclassifies a bit / national GROUP — which §13.18.29.4 GR1b/GR2b make
    /// elementary boolean / national items — as a category-less alphanumeric group. The ONE reader is
    /// <c>OperandPic</c> (<c>Pic ?? AsIfPic</c>), reached through <c>StringCategoryOf</c>.
    /// <para>Only four categories may still be tested through <c>Pic</c>, and only because no group can carry
    /// them: object reference, pointer, program-pointer (the §8.8.4.2.14/.15/.16 identity relations) and numeric.
    /// Plus ONE named exemption — <c>IsNationalOperand</c>'s field arm, the class-condition classification test,
    /// whose national blind spot is a documented hole reported with PB741 and owned by the §8.8.4.4 mechanism.
    /// A new <c>Pic?.Category</c> read on any other category turns this red, which is exactly the shape that made
    /// a level-88 over a national group take the alphanumeric weight table.</para></summary>
    [Fact]
    public void NoNewCategoryRead_GoesThroughPic_WhereAGroupCanReachIt()
    {
        const string exemption = "BoundFieldOperand { Place.Item.Pic.Category: PicCategory.National } => true,";
        string[] groupProofCategories =
            ["PicCategory.ObjectReference", "PicCategory.Pointer", "PicCategory.ProgramPointer", "PicCategory.Numeric"];

        var lines = File.ReadAllLines(Path.Combine(TestRepo.Root,
            "src", "Cobol.Net.Compiler", "CodeGen", "Emit", "ConditionRenderer.cs"));
        int exemptions = 0, checkedReads = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)
                || line.TrimStart().StartsWith("///", StringComparison.Ordinal))
                continue;
            if (!line.Contains("Item.Pic?.Category", StringComparison.Ordinal)
                && !line.Contains("Item.Pic.Category", StringComparison.Ordinal))
                continue;
            if (line.Contains(exemption, StringComparison.Ordinal)) { exemptions++; continue; }
            checkedReads++;
            Assert.True(groupProofCategories.Any(c => line.Contains(c, StringComparison.Ordinal)),
                $"ConditionRenderer.cs:{i + 1} reads a category through `Pic`, which is NULL for every group: "
                + "`{line.Trim()}`. Read `OperandPic` (or StringCategoryOf) instead — §13.18.29.4 GR1b/GR2b make a "
                + "bit / national group an elementary boolean / national item, and `Pic` cannot see that "
                + "(kb/Work PB728).");
        }
        Assert.Equal(1, exemptions);      // the class-condition hole, and only it
        Assert.True(checkedReads >= 4, $"expected the four group-proof category tests, found {checkedReads}");
    }

    /// <summary>END TO END, over the NC215A shape. <c>ALPHABET AL IS "9"</c> puts '9' at ordinal position 1 and,
    /// by §12.3.7.4 GR7 k)3 ("Any characters of the native collating sequence that are not specified in the literal
    /// phrase shall assume a position in the collating sequence that is greater than that of the highest character
    /// specified in this literal phrase"), every other character — the space and the quotation mark included —
    /// above it. §12.3.6.4 GR9 makes AL the alphanumeric program collating sequence. So each leg below is TRUE:
    /// §8.3.3.6.4 GR1 makes SPACE/QUOTE alphanumeric character values, §8.8.4.2.5 makes the comparison alphanumeric
    /// (the one-digit integer moved to a one-position alphanumeric item — "9"), and §8.8.4.2.7 collates it under AL.
    /// <para>The figurative legs and their literal twins MUST agree: they are the same §8.8.4.2.5 comparison
    /// written two ways. Before the fix FIG/QUO/SIG answered N while LIT/SLI answered Y.</para>
    /// <para>SIG/SLI additionally pin §14.9.25.4 GR6a through §8.8.4.2.5's move: "If the sending operand is
    /// described as being signed numeric, the operational sign is not moved" — so PIC S9 compares "9".</para></summary>
    [Fact]
    public void ANumericDisplayOperandAgainstAFigurative_ReadsTheProgramCollatingSequence()
    {
        const string src = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB741PCSTWIN.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               OBJECT-COMPUTER. XX PROGRAM COLLATING SEQUENCE AL.
               SPECIAL-NAMES. ALPHABET AL IS "9".
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 N9 PIC 9 VALUE 9.
               01 S9 PIC S9 VALUE 9.
               PROCEDURE DIVISION.
               MAIN.
                   IF N9 < SPACE DISPLAY "FIG=Y" ELSE DISPLAY "FIG=N" END-IF
                   IF N9 < " "   DISPLAY "LIT=Y" ELSE DISPLAY "LIT=N" END-IF
                   IF N9 < QUOTE DISPLAY "QUO=Y" ELSE DISPLAY "QUO=N" END-IF
                   IF N9 < ALL "  " DISPLAY "ALL=Y" ELSE DISPLAY "ALL=N" END-IF
                   IF S9 < SPACE DISPLAY "SIG=Y" ELSE DISPLAY "SIG=N" END-IF
                   IF S9 < " "   DISPLAY "SLI=Y" ELSE DISPLAY "SLI=N" END-IF
                   STOP RUN.
            """;
        var (ok, stdout, detail) = CompileAndRun(src);
        Assert.True(ok, detail);
        Assert.Equal("FIG=Y\r\nLIT=Y\r\nQUO=Y\r\nALL=Y\r\nSIG=Y\r\nSLI=Y", stdout);
    }

    /// <summary>The same rule on the LEVEL-88 surface (§8.8.4.5.3 GR2 — "The rules for comparing a conditional
    /// variable with a condition-name value are the same as those specified for relation conditions") and on the
    /// EVALUATE THRU surface. A NATIONAL GROUP conditional variable carries its as-if PICTURE on
    /// <c>OperandPic</c> (§13.18.29.4 GR2b) and has <c>Pic</c> null, so reading <c>Pic</c> handed it the
    /// ALPHANUMERIC weight table: under <c>ALPHABET AL IS "A" ALSO "B"</c> the national 'A' and 'B' share one
    /// position, and the group answered TRUE where its elementary twin — correctly under the NATIONAL sequence,
    /// where they are positions 0 and 1 — answered FALSE (measured, kb/Work PB741 sweep).</summary>
    [Fact]
    public void ANationalGroupConditionalVariable_ReadsTheNationalSequence_LikeItsElementaryTwin()
    {
        const string src = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB741NATGRP88.
               ENVIRONMENT DIVISION.
               CONFIGURATION SECTION.
               OBJECT-COMPUTER. XX
                   PROGRAM COLLATING SEQUENCE
                       FOR ALPHANUMERIC IS AL
                       FOR NATIONAL IS NAT3.
               SPECIAL-NAMES.
                   ALPHABET AL IS "A" ALSO "B" ALSO "C"
                   ALPHABET NAT3 FOR NATIONAL IS N"ABC".
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 NG GROUP-USAGE NATIONAL.
                  88 IS-A VALUE N"A".
                  05 NA PIC N(1).
               01 NE PIC N(1) VALUE N"B".
                  88 IS-A-E VALUE N"A".
               PROCEDURE DIVISION.
               MAIN.
                   MOVE N"B" TO NA
                   IF IS-A   DISPLAY "GRP=Y" ELSE DISPLAY "GRP=N" END-IF
                   IF IS-A-E DISPLAY "ELM=Y" ELSE DISPLAY "ELM=N" END-IF
                   STOP RUN.
            """;
        var (ok, stdout, detail) = CompileAndRun(src, dialectLevel: 2002);
        Assert.True(ok, detail);
        Assert.Equal("GRP=N\r\nELM=N", stdout);
    }
}
