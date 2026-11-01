// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ A PROCEDURE-DIVISION DECISION LOOKS NO FURTHER THAN ONE HEADER, HOWEVER MANY PARAGRAPHS A SECTION HOLDS (kb/Work
/// PB4230).
///
/// <para><b>Why it exists.</b> Were <c>CobolParserCore.procedureDivision</c> one loop over
/// <c>(sectionDefinition | paragraphDefinition)</c>, a paragraph after a section header could stay in the section or
/// close its <c>paragraphDefinition*</c> loop and be the division's own. Both derivations accept every token to the
/// next section header or the end of the division, so full-context LL prediction (the pass every program whose SLL
/// pass bails takes, <c>Frontend.ParsePass</c>) scans that far at EVERY paragraph header: a legal 400-paragraph section
/// with one <c>SET p TO NULL</c> compiled in 112 s that way, CCVS DB201A in 58 s. ISO §14.4.2 says which reading is
/// right — "A section consists of a section header followed by zero, one, or more successive paragraphs. A section
/// ends immediately before the next section or at the end of the procedure division" — and the grammar writes it as
/// its shape (<c>sentence* paragraphDefinition* sectionDefinition*</c>), so no prediction choice is left to make.</para>
///
/// <para><b>What it measures.</b> The same program at two section lengths is parsed in LL mode under ANTLR's
/// profiler. Every decision of the procedure-division structure rules must look ahead no further at the longer
/// length than at the shorter, and no further than one qualified section header (<c>S OF Q SECTION 12 .</c> plus
/// the token that ends the prediction); none may report an ambiguity. A restructuring that makes a loop's exit
/// depend on what follows the next paragraph fails here, not as a compile that grows with the square of the
/// program.</para>
/// </summary>
public sealed class ProcedureDivisionShapeDriftTests
{
    /// <summary>The rules whose decisions decide where a procedure-division paragraph or section belongs.</summary>
    private static readonly int[] StructureRules =
    [
        Core.RULE_procedureDivision, Core.RULE_declarativePart, Core.RULE_declarativeSection,
        Core.RULE_declarativeParagraph, Core.RULE_sectionDefinition, Core.RULE_paragraphDefinition,
    ];

    /// <summary>The longest look one header needs: <c>S OF Q SECTION 12 .</c> (six tokens) and the token after it.</summary>
    private const int OneHeader = 7;

    [Fact]
    public void ProcedureDivisionDecisions_LookAheadIndependentlyOfTheSectionLength()
    {
        var shortLook = MaxLook(paragraphsPerSection: 3);
        var longLook = MaxLook(paragraphsPerSection: 60);

        foreach (var (rule, look) in longLook)
        {
            Assert.True(look.Ambiguities == 0,
                $"{Core.ruleNames[rule]}: {look.Ambiguities} ambiguity report(s) under LL prediction");
            Assert.True(look.Max <= OneHeader,
                $"{Core.ruleNames[rule]}: a decision looked {look.Max} tokens ahead, beyond one header ({OneHeader})");
            Assert.True(look.Max <= shortLook[rule].Max,
                $"{Core.ruleNames[rule]}: a decision looked {look.Max} tokens ahead with 60 paragraphs per section and "
                + $"{shortLook[rule].Max} with 3 — its prediction grows with the section");
        }
    }

    /// <summary>Every structure rule's deepest SLL or LL look, and its ambiguity reports, over one LL-mode parse of a
    /// program with DECLARATIVES, leading paragraphs refused by name elsewhere (COBOL2797), two sections of
    /// <paramref name="paragraphsPerSection"/> paragraphs, one of them led by unnamed sentences, and a nested
    /// program after them.</summary>
    private static Dictionary<int, (long Max, int Ambiguities)> MaxLook(int paragraphsPerSection)
    {
        var text = new StringBuilder("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SHAPE.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 W-A PIC 9(5).
            PROCEDURE DIVISION.
            DECLARATIVES.
            D1 SECTION.
                USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
            D1-P.
                EXIT.
            END DECLARATIVES.
            LEAD-P.
                MOVE 0 TO W-A.

            """);
        foreach (string section in (string[])["S1", "S2"])
        {
            text.Append($"{section} SECTION.\n");
            if (section == "S2") text.Append("    ADD 1 TO W-A.\n");
            for (int n = 0; n < paragraphsPerSection; n++)
                text.Append($"{section}-P{n}.\n    MOVE 1 TO W-A.\n");
        }
        text.Append("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. INNER.
            PROCEDURE DIVISION.
            I-P.
                EXIT PROGRAM.
            END PROGRAM INNER.
            END PROGRAM SHAPE.
            """);

        var parser = new Core(new CommonTokenStream(new CobolLexer(new AntlrInputStream(text.ToString()))))
            { Edition = EditionInfo.Of(2023) };
        parser.Profile = true;   // installs the profiling simulator, so the prediction mode is set on it afterwards
        parser.Interpreter.PredictionMode = PredictionMode.LL;
        var errors = new SyntaxErrors();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(errors);
        parser.compilationUnit();
        Assert.Empty(errors.Messages);

        var looks = StructureRules.ToDictionary(rule => rule, _ => (Max: 0L, Ambiguities: 0));
        foreach (var decision in parser.ParseInfo.getDecisionInfo())
        {
            int rule = parser.Atn.decisionToState[decision.decision].ruleIndex;
            if (!looks.TryGetValue(rule, out var look)) continue;
            looks[rule] = (Math.Max(look.Max, Math.Max(decision.SLL_MaxLook, decision.LL_MaxLook)),
                look.Ambiguities + decision.ambiguities.Count);
        }
        return looks;
    }

    /// <summary>The parse's syntax errors, as <c>line:column message</c>, so a program the grammar stops accepting
    /// names its error instead of a count.</summary>
    private sealed class SyntaxErrors : BaseErrorListener
    {
        public List<string> Messages { get; } = [];

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e) =>
            Messages.Add($"{line}:{charPositionInLine} {msg}");
    }
}
