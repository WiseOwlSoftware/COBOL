// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Antlr4.Runtime;
using Antlr4.Runtime.Atn;
using Antlr4.Runtime.Misc;
using WiseOwl.Cobol.Editions;
using WiseOwl.Cobol.Frontend.Generated;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

using Core = CobolParserCore;

/// <summary>
/// ⛔ EVERY <c>SET … TO</c> SENDER IS PREDICTED BY SLL, SO A LEGAL PROGRAM IS PARSED ONCE (kb/Work PB4231).
///
/// <para><b>Why it exists.</b> <c>Frontend.ParsePass</c> parses with SLL prediction and <c>BailErrorStrategy</c> first and
/// re-parses the WHOLE file under full LL when that pass bails. The grammar used to split the one shape
/// <c>SET receivers TO sender</c> of ISO §14.9.39.2 across three <c>setStatement</c> alternatives by the sender's form,
/// and all three could consume <c>NULL</c>: after <c>SET X TO NULL</c> SLL held them at shared ATN states, ANTLR's
/// conflict heuristic took the lowest (the arithmetic one, which needs <c>AS</c> or <c>::</c> after NULL), and the
/// parse died at the next token. 75 of the corpus's legal programs took the LL pass for that alone, and decision
/// <c>setStatement</c> reported 125 context sensitivities on it. <c>setToStatement</c> is now the one rule, and its
/// sender decides its arm at the left edge, NULL first (<c>PrintedFormatAlternativeDriftTests</c> holds the arm
/// order).</para>
///
/// <para><b>What it measures.</b> One program writes every <c>SET … TO</c> shape the one rule and its keyword-led
/// neighbours carry — NULL to a pointer, to a receiver list and to an <c>ADDRESS OF</c> receiver, the data-address
/// sender, a mixed Format-7 receiver list, NULL viewed AS a class and NULL invoked, SELF, an index value, a condition,
/// the function-, program- and ENTRY senders — each followed by a DIFFERENT next token (a period or the verbs the
/// corpus's bails stopped at). Parsed exactly as the SLL pass parses, it must not bail; parsed in LL mode under ANTLR's
/// profiler, no decision of the SET rules may report an ambiguity or a context sensitivity (a decision SLL cannot make
/// without the call stack).</para>
/// </summary>
public sealed class SetToStatementSllDriftTests
{
    /// <summary>The rules whose decisions choose a SET format, its receivers and its sender.</summary>
    private static readonly int[] SetRules =
        [Core.RULE_setStatement, Core.RULE_setToStatement, Core.RULE_setToReceiver, Core.RULE_setToSender];

    private const string Program = "IDENTIFICATION DIVISION.\n"
        + "PROGRAM-ID. SETSLL.\n"
        + "DATA DIVISION.\n"
        + "WORKING-STORAGE SECTION.\n"
        + "01 P1 USAGE POINTER.\n"
        + "01 P2 USAGE POINTER.\n"
        + "01 R1 PIC X(4).\n"
        + "01 B1 PIC X(4) BASED.\n"
        + "01 O1 USAGE OBJECT REFERENCE.\n"
        + "01 N1 PIC 9(4).\n"
        + "01 PP USAGE PROGRAM-POINTER.\n"
        + "01 FLG PIC X.\n"
        + "   88 FLG-ON VALUE \"Y\".\n"
        + "PROCEDURE DIVISION.\n"
        + "MAIN-S SECTION.\n"
        + "P-A.\n"
        + "    SET P1 TO NULL.\n"
        + "    SET P1 TO NULL\n"
        + "    SET P1 P2 TO NULL\n"
        + "    IF P1 = NULL DISPLAY \"N\" END-IF\n"
        + "    SET ADDRESS OF B1 TO NULL\n"
        + "    MOVE 1 TO N1\n"
        + "    SET P1 TO ADDRESS OF R1\n"
        + "    SET ADDRESS OF B1 TO P1\n"
        + "    CALL \"SUB\" USING P1\n"
        + "    SET ADDRESS OF B1 P2 TO ADDRESS R1\n"
        + "    SET O1 TO NULL\n"
        + "    INVOKE O1 \"M\"\n"
        + "    SET O1 TO NULL AS C1\n"
        + "    SET O1 TO NULL :: \"M\"\n"
        + "    SET O1 TO SELF\n"
        + "    SET N1 TO 3\n"
        + "    SET FLG-ON TO TRUE\n"
        + "    SET P1 TO ADDRESS OF FUNCTION F1\n"
        + "    SET PP TO ADDRESS OF PROGRAM \"SUB\"\n"
        + "    SET PP TO ENTRY \"SUB\"\n"
        + "    SET PP TO NULL\n"
        + "    GOBACK.\n"
        + "END PROGRAM SETSLL.\n";

    [Fact]
    public void EverySetToSender_IsPredictedBySll()
    {
        var parser = Parser();
        parser.Interpreter.PredictionMode = PredictionMode.SLL;
        parser.ErrorHandler = new BailErrorStrategy();
        parser.RemoveErrorListeners();
        try
        {
            parser.compilationUnit();
        }
        catch (ParseCanceledException e)
        {
            var at = (e.InnerException as RecognitionException)?.OffendingToken;
            Assert.Fail($"the SLL pass bailed at line {at?.Line}, token '{at?.Text}': a SET … TO sender's arm is not "
                + "decidable without the call stack, so every program that writes it is parsed twice (kb/Work PB4231)");
        }
    }

    [Fact]
    public void SetDecisions_ReportNoAmbiguityOrContextSensitivity()
    {
        var parser = Parser();
        parser.Profile = true;   // installs the profiling simulator, so the prediction mode is set on it afterwards
        parser.Interpreter.PredictionMode = PredictionMode.LL;
        var errors = new List<string>();
        parser.RemoveErrorListeners();
        parser.AddErrorListener(new Collect(errors));
        parser.compilationUnit();
        Assert.Empty(errors);

        int invoked = 0;
        foreach (var decision in parser.ParseInfo.getDecisionInfo())
        {
            int rule = parser.Atn.decisionToState[decision.decision].ruleIndex;
            if (!SetRules.Contains(rule)) continue;
            invoked += (int)decision.invocations;
            Assert.True(decision.ambiguities.Count == 0,
                $"{Core.ruleNames[rule]} (decision {decision.decision}): {decision.ambiguities.Count} ambiguity report(s)");
            Assert.True(decision.contextSensitivities.Count == 0,
                $"{Core.ruleNames[rule]} (decision {decision.decision}): {decision.contextSensitivities.Count} context "
                + "sensitivit(ies) — SLL predicts one arm and full LL another, which is a bail on the SLL pass");
        }
        Assert.True(invoked > 0, "no SET decision was invoked — the program no longer reaches the SET rules");
    }

    private static Core Parser() =>
        new(new CommonTokenStream(new CobolLexer(new AntlrInputStream(Program)))) { Edition = EditionInfo.Of(2023) };

    private sealed class Collect(List<string> messages) : BaseErrorListener
    {
        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int charPositionInLine, string msg, RecognitionException e) =>
            messages.Add($"{line}:{charPositionInLine} {msg}");
    }
}
