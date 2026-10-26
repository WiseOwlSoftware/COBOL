// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using CobolNet.Tests.Shared;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ AN IMPERATIVE-STATEMENT OPERAND THE GENERAL FORMAT LEAVES UNBRACKETED IS REQUIRED, AND THE QUANTIFIER IS
/// WHERE THAT RULE LIVES (kb/Work PB396).
///
/// <para><b>The rule, one sentence.</b> ISO §5.2.6.2 gives the omission licence to BRACKETED portions only —
/// "Brackets, [ ], enclosing a portion of a general format indicate that the syntax element contained within the
/// brackets … may be explicitly specified or that portion of the general format may be omitted" — and §5.2.6.3
/// says a brace group "shall be explicitly specified". §14.9.19.3 SR1 states the same cardinality a second way
/// for IF: "Statement-1 and statement-2 represent either one or more imperative statements or a conditional
/// statement optionally preceded by one or more imperative statements".</para>
///
/// <para><b>What was measured before the fix.</b> Every USE of <c>statementBlock</c> in the control-flow grammar
/// was written <c>statementBlock*</c>, which restores the zero case the printed formats forbid, so
/// <c>IF X = 1 END-IF</c>, a WHEN phrase with no body, <c>PERFORM UNTIL … END-PERFORM</c> with no body and a
/// SEARCH WHEN with no body all compiled SILENTLY at every edition — the PERFORM case into a loop whose
/// condition its (absent) body cannot change.</para>
///
/// <para><b>Why the guard is over the GRAMMAR SOURCE and not over a list of verbs.</b> The requirement is a
/// property of the quantifier, so the drift test asserts the quantifier: no <c>statementBlock*</c> (and no
/// <c>statementBlock?</c>) anywhere in the grammar. That makes the NEXT general format automatic — a new phrase
/// rule written with the zero-admitting quantifier fails here — which a per-verb list could never do.</para>
/// </summary>
public sealed class RequiredImperativeStatementDriftTests : CobolNetTestBase
{
    private static string GrammarDir()
        => Path.Combine(TestRepo.Root, "src", "Cobol.Net.Frontend", "Grammar");

    /// <summary>Grammar source with <c>//</c> line comments removed — the prose in this repo's .g4 files QUOTES
    /// the forbidden spelling when it explains why it is forbidden, and a guard that cannot tell a rule from its
    /// explanation would force the explanation out.</summary>
    private static IEnumerable<(string Path, string Text)> GrammarFiles()
    {
        foreach (string p in Directory.EnumerateFiles(GrammarDir(), "*.g4", SearchOption.AllDirectories))
            yield return (p, Regex.Replace(File.ReadAllText(p), @"//[^\n]*", ""));
    }

    /// <summary>⛔ THE INVARIANT. <c>statementBlock</c> IS <c>statement+</c>, so a bare reference already spells
    /// "one or more" — the format's own cardinality. A <c>*</c> or <c>?</c> on it spells "zero or more", which no
    /// unbracketed operand and no brace alternative in any general format means.</summary>
    [Fact]
    public void NoGrammarRule_AdmitsAnEmptyStatementBlock()
    {
        var files = GrammarFiles().ToList();
        Assert.True(files.Count >= 8, $"only {files.Count} .g4 files found under {GrammarDir()} — the scrape broke");

        var offenders = new List<string>();
        foreach (var (path, text) in files)
            foreach (Match m in Regex.Matches(text, @"statementBlock\s*[*?]"))
                offenders.Add($"{Path.GetFileName(path)}: {m.Value.Trim()}");

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} grammar reference(s) admit an EMPTY statement block: {string.Join(", ", offenders)}. "
            + "ISO §5.2.6.2 licenses an omission only for a BRACKETED portion and §5.2.6.3 requires one alternative "
            + "of a brace group to be explicitly specified, so an imperative-statement operand is required wherever "
            + "the printed general format leaves it outside brackets (kb/Work PB396).");
    }

    /// <summary>The population the invariant governs must not be empty — a guard that measures nothing is green
    /// for the wrong reason. There are nine such operand positions across IF, EVALUATE, PERFORM and SEARCH.</summary>
    [Fact]
    public void TheGrammar_StillUsesStatementBlockAtEveryPhrasePosition()
    {
        int uses = GrammarFiles().Sum(f => Regex.Matches(f.Text, @"\bstatementBlock\b").Count);
        Assert.True(uses >= 30, $"only {uses} statementBlock references — the rule was renamed or the phrases were "
            + "rewritten; this guard must move with them rather than pass vacuously");
    }

    /// <summary>⛔ WHEN OTHER IS ONE OPTIONAL PHRASE THAT FOLLOWS THE REPETITION, not a second alternative of the
    /// repeated clause (ISO §14.9.13.2, with §5.2.7: an ellipsis "applies to the portion of the format between
    /// the determined pair of delimiters" — the brace group immediately to its left, which the OTHER phrase is
    /// outside of). Written as an alternative it was admissible any number of times at any position, and
    /// <c>EvaluateBinder</c>'s <c>other = body</c> silently discarded every earlier one.</summary>
    [Fact]
    public void EvaluateWhenOther_IsASingleTrailingPhrase_NotAnAlternativeOfTheRepeatedClause()
    {
        string text = GrammarFiles().Single(f => f.Path.EndsWith("CobolControlFlow.g4", StringComparison.Ordinal)).Text;

        var clause = Regex.Match(text, @"^evaluateWhenClause\s*\r?\n\s*:(?<body>.*?);",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(clause.Success, "evaluateWhenClause not found");
        Assert.DoesNotContain("OTHER", clause.Groups["body"].Value, StringComparison.Ordinal);

        var stmt = Regex.Match(text, @"^evaluateStatement\s*\r?\n\s*:(?<body>.*?);",
            RegexOptions.Multiline | RegexOptions.Singleline);
        Assert.True(stmt.Success, "evaluateStatement not found");
        string body = stmt.Groups["body"].Value;
        int clausePos = body.IndexOf("evaluateWhenClause+", StringComparison.Ordinal);
        int otherPos = body.IndexOf("evaluateWhenOther?", StringComparison.Ordinal);
        Assert.True(clausePos >= 0, $"evaluateStatement no longer repeats evaluateWhenClause: {body}");
        Assert.True(otherPos > clausePos,
            $"evaluateWhenOther? must follow evaluateWhenClause+ — the printed format puts it after the `{{ … }} …` "
            + $"repetition, so it is admitted once and last (ISO §14.9.13.2 / §5.2.7). Body: {body}");
    }

    // ── The nine operand positions, end to end ───────────────────────────────────────────────────────────

    private const string Head =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {0}.\n       DATA DIVISION.\n"
        + "       WORKING-STORAGE SECTION.\n       01 X PIC 9(4) VALUE 1.\n       01 N PIC 9(4) VALUE 0.\n"
        + "       01 T.\n          05 R PIC 9(4) OCCURS 3 TIMES INDEXED BY I.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-P.\n";

    private static string Program(string id, string body) => string.Format(Head, id) + body + "\n           STOP RUN.\n";

    /// <summary>Every unbracketed imperative-statement operand, written EMPTY, is rejected — and rejected with
    /// the named diagnostic rather than a generic parse failure, so the user is told which rule was broken.
    /// COBOL2072 is the empty operand; COBOL2073 is the WHEN OTHER phrase written out of position, which is
    /// the same format read one level up.</summary>
    [Theory]
    // IF Format 1 / Format 2 — statement-1 unbracketed, and a brace alternative (ISO §14.9.19.2).
    [InlineData("PB396A", "           IF X = 1\n           END-IF\n           DISPLAY \"A\"", "COBOL2072")]
    [InlineData("PB396B", "           IF X = 1\n               DISPLAY \"T\"\n           ELSE\n           END-IF", "COBOL2072")]
    [InlineData("PB396C", "           IF X = 1.\n           DISPLAY \"A\".", "COBOL2072")]
    // EVALUATE — imperative-statement-1 inside the outer brace group (ISO §14.9.13.2).
    [InlineData("PB396D", "           EVALUATE X\n               WHEN 3\n           END-EVALUATE", "COBOL2072")]
    [InlineData("PB396E", "           EVALUATE X\n               WHEN 3 DISPLAY \"T\"\n               WHEN OTHER\n"
                        + "           END-EVALUATE", "COBOL2072")]
    // …and the WHEN OTHER phrase out of position: repeated, or ahead of a WHEN phrase.
    [InlineData("PB396F", "           EVALUATE X\n               WHEN OTHER DISPLAY \"O1\"\n"
                        + "               WHEN 3 DISPLAY \"T\"\n           END-EVALUATE", "COBOL2073")]
    [InlineData("PB396G", "           EVALUATE X\n               WHEN 3 DISPLAY \"T\"\n"
                        + "               WHEN OTHER DISPLAY \"O1\"\n               WHEN OTHER DISPLAY \"O2\"\n"
                        + "           END-EVALUATE", "COBOL2073")]
    // PERFORM Format 2 — imperative-statement-1 unbracketed between the phrase bracket and END-PERFORM.
    [InlineData("PB396H", "           PERFORM UNTIL X > 5\n           END-PERFORM", "COBOL2072")]
    // SEARCH / SEARCH ALL — `{ imperative-statement-2 | NEXT SENTENCE }`, a brace alternation (ISO §14.9.37.2).
    [InlineData("PB396J", "           SET I TO 1\n           SEARCH R\n               AT END DISPLAY \"NF\"\n"
                        + "               WHEN R (I) = 7\n           END-SEARCH", "COBOL2072")]
    public void AnEmptyRequiredImperative_IsRejected(string id, string body, string code)
    {
        var (ok, _, detail) = CompileAndRun(Program(id, body));
        Assert.False(ok, $"{id} compiled and ran: an imperative-statement the general format leaves unbracketed "
            + "was accepted empty (ISO §5.2.6.2 / §5.2.6.3).");
        Assert.Contains(code, detail, StringComparison.Ordinal);
    }

    /// <summary>The wholly empty inline PERFORM — no loop-control phrase, no body — is REJECTED with the named
    /// COBOL2072, which is the rule (§14.9.28.2 Format 2 prints imperative-statement-1 unbracketed). It was the one
    /// shape that drew only the generic parse error: with no <c>performInlineHead</c> to commit the parse, the failure is
    /// a no-viable-alternative on <c>performStatement</c>'s own alternatives decision, with the parser still before
    /// PERFORM, and the old test read the PARSER's expected set there. The test now re-simulates the parse to the
    /// offending END-PERFORM and asks whether one statement there would let it follow (kb/Work PB3832).</summary>
    [Fact]
    public void AWhollyEmptyInlinePerform_IsRejected_WithTheNamedDiagnostic()
    {
        var (ok, _, detail) = CompileAndRun(Program("PB396I", "           PERFORM\n           END-PERFORM"));
        Assert.False(ok, "an inline PERFORM with no imperative-statement-1 was accepted (ISO §14.9.28.2 Format 2).");
        Assert.Contains("COBOL2072", detail, StringComparison.Ordinal);
        Assert.Contains("The PERFORM statement's general format", detail, StringComparison.Ordinal);
    }

    // ── kb/Work PB3832: the empty-block diagnostic names the real fault at every nesting depth ──────────────

    /// <summary>The r14 shape: an <c>IF … ELSE IF … END-IF</c> that leaves the OUTER IF open, so the inner IF's
    /// optional END-IF is a dangling decision whose full-context prediction looks ahead through the statements that
    /// follow. A failure there is reported at the token where every alternative died, with the parser still at the
    /// END-IF.</summary>
    private const string OpenNest =
        "           IF X = 1\n               DISPLAY \"A\"\n           ELSE IF X = 2\n               DISPLAY \"B\"\n"
        + "           END-IF\n";

    /// <summary>A statement that fails INSIDE a block: LENGTH is reserved (§8.9), and `LENGTH OF` is no ISO
    /// register, so the condition fails at OF.</summary>
    private const string FailsInside = "IF LENGTH OF X = 4 DISPLAY \"C\" END-IF";

    /// <summary>⛔ A TRULY EMPTY BLOCK DRAWS COBOL2072 NAMING ITS OWN STATEMENT, AT DEPTH 0 AND INSIDE THE OPEN NEST.
    /// Before kb/Work PB3832 the nested rows named IF for every construct (the parser's rule stack at the dangling
    /// END-IF), and the empty inline PERFORM drew only the generic error. ISO §5.2.6.2 licenses an omission only for a
    /// bracketed portion; each block here is unbracketed in its format (§14.9.19.2 IF, §14.9.13.2 EVALUATE, §14.9.28.2
    /// PERFORM, §14.9.37.2 SEARCH, §14.9.2.2 ADD's SIZE ERROR phrase), and each block's own scope terminator closes a
    /// statement that IS open (§14.5.3.2). The WHEN OTHER row runs nested only: at depth 0 the parse fails at the WHEN,
    /// where the WHEN OTHER arm names the phrase itself (row PB396E of <see cref="AnEmptyRequiredImperative_IsRejected"/>).
    /// The AH..AJ rows are review R1-1's: the block's statement stands more than sixteen tokens past the dangling END-IF
    /// decision, where the walk used to give up and the named diagnostic fell silent; the decision looks ahead to the
    /// sentence's end, and so does the walk.</summary>
    [Theory]
    [InlineData("PB3832A", false, "IF X = 1 END-IF", "IF")]
    [InlineData("PB3832B", true, "IF X = 1 END-IF", "IF")]
    [InlineData("PB3832C", false, "IF X = 1 DISPLAY \"T\" ELSE END-IF", "IF")]
    [InlineData("PB3832D", true, "IF X = 1 DISPLAY \"T\" ELSE END-IF", "IF")]
    [InlineData("PB3832E", false, "EVALUATE X WHEN 3 END-EVALUATE", "EVALUATE")]
    [InlineData("PB3832F", true, "EVALUATE X WHEN 3 END-EVALUATE", "EVALUATE")]
    [InlineData("PB3832G", false, "PERFORM END-PERFORM", "PERFORM")]
    [InlineData("PB3832H", true, "PERFORM END-PERFORM", "PERFORM")]
    [InlineData("PB3832I", false, "PERFORM UNTIL X > 5 END-PERFORM", "PERFORM")]
    [InlineData("PB3832J", true, "PERFORM UNTIL X > 5 END-PERFORM", "PERFORM")]
    [InlineData("PB3832K", false, "SEARCH R WHEN R (I) = 7 END-SEARCH", "SEARCH")]
    [InlineData("PB3832L", true, "SEARCH R WHEN R (I) = 7 END-SEARCH", "SEARCH")]
    [InlineData("PB3832M", true, "ADD 1 TO X ON SIZE ERROR END-ADD", "ADD")]
    [InlineData("PB3832M0", false, "ADD 1 TO X ON SIZE ERROR END-ADD", "ADD")]
    [InlineData("PB3832AD", true, "ADD 1 TO X NOT ON SIZE ERROR END-ADD", "ADD")]
    [InlineData("PB3832AD0", false, "ADD 1 TO X NOT ON SIZE ERROR END-ADD", "ADD")]
    [InlineData("PB3832AH", true, "PERFORM UNTIL X = 1 AND X = 2 AND X = 3 AND X = 4 AND X = 5 AND X = 6 END-PERFORM", "PERFORM")]
    [InlineData("PB3832AI", true, "IF X = 1 AND X = 2 AND X = 3 AND X = 4 AND X = 5 AND X = 6 END-IF", "IF")]
    [InlineData("PB3832AJ", true, "MOVE X TO N N N N N N N N N N N N N N N N N\n           IF X = 1 END-IF", "IF")]
    [InlineData("PB3832AE", false, "SEARCH R AT END WHEN R (I) = 7 DISPLAY \"F\" END-SEARCH", "SEARCH")]
    [InlineData("PB3832AF", true, "SEARCH R AT END WHEN R (I) = 7 DISPLAY \"F\" END-SEARCH", "SEARCH")]
    [InlineData("PB3832AG", true, "EVALUATE X WHEN 3 DISPLAY \"T\" WHEN OTHER END-EVALUATE", "EVALUATE")]
    public void ATrulyEmptyBlock_IsNamedByItsOwnStatement_AtEveryDepth(string id, bool nested, string statement, string owner)
    {
        var (ok, _, detail) = CompileAndRun(Program(id, (nested ? OpenNest : "") + "           " + statement), 2023);
        Assert.False(ok, $"{id}: an empty imperative-statement compiled");
        Assert.Contains("COBOL2072", detail, StringComparison.Ordinal);
        Assert.Contains($"The {owner} statement's general format", detail, StringComparison.Ordinal);
        foreach (string other in new[] { "IF", "EVALUATE", "PERFORM", "SEARCH", "ADD" }.Where(o => o != owner))
            Assert.DoesNotContain($"The {other} statement's general format", detail, StringComparison.Ordinal);
        // The scope-terminator hint asked the parser's rule stack too: nested, the statement opened after the dangling
        // END-IF decision was on no stack of the parser's, and its own terminator was "without a matching" statement.
        Assert.DoesNotContain("appears without a matching", detail, StringComparison.Ordinal);
    }

    /// <summary>⛔ A BLOCK THAT HOLDS A FAILING STATEMENT IS NOT EMPTY, AT ANY DEPTH (kb/Work PB3832). Each block below
    /// begins with a statement that fails inside its condition, and each nested row used to draw COBOL2072, "the IF
    /// statement's general format shows an imperative-statement here", where the IF has its statement. The condition
    /// rows are the second witness: the fault of <c>IF X NOT &lt;&gt; 1</c> is the NOT that §8.8.4.2.2 Format 1 does
    /// not print before <c>&lt;&gt;</c>, and the old test read the IF's next position, after the condition it had
    /// closed at X, as an empty block. None of these is a missing statement, so none draws COBOL2072; and since kb/Work
    /// PB3832's review (R1-2) each draws exactly the one diagnostic the lone failing statement draws, at the token where
    /// the parse dies, at both depths: the depth-0 SIZE ERROR row used to draw COBOL0307 "A period may be missing" at ON,
    /// a phrase ISO §14.9.2.2 prints, because the parse closed the ADD before it.</summary>
    [Theory]
    [InlineData("PB3832N", false, "IF X = 1 " + FailsInside + " END-IF", "OF")]
    [InlineData("PB3832O", true, "IF X = 1 " + FailsInside + " END-IF", "OF")]
    [InlineData("PB3832P", false, "IF X = 1 DISPLAY \"T\" ELSE " + FailsInside + " END-IF", "OF")]
    [InlineData("PB3832Q", true, "IF X = 1 DISPLAY \"T\" ELSE " + FailsInside + " END-IF", "OF")]
    [InlineData("PB3832R", false, "EVALUATE X WHEN 3 " + FailsInside + " END-EVALUATE", "OF")]
    [InlineData("PB3832S", true, "EVALUATE X WHEN 3 " + FailsInside + " END-EVALUATE", "OF")]
    [InlineData("PB3832T", false, "PERFORM " + FailsInside + " END-PERFORM", "OF")]
    [InlineData("PB3832U", true, "PERFORM " + FailsInside + " END-PERFORM", "OF")]
    [InlineData("PB3832V", false, "SEARCH R WHEN R (I) = 7 " + FailsInside + " END-SEARCH", "OF")]
    [InlineData("PB3832W", true, "SEARCH R WHEN R (I) = 7 " + FailsInside + " END-SEARCH", "OF")]
    [InlineData("PB3832X", false, "ADD 1 TO X ON SIZE ERROR " + FailsInside + " END-ADD", "OF")]
    [InlineData("PB3832Y", true, "ADD 1 TO X ON SIZE ERROR " + FailsInside + " END-ADD", "OF")]
    // A WHEN OTHER phrase in place whose statement fails: the WHEN OTHER arm (0b) read the present body as a misplaced
    // phrase (COBOL2073) at depth 0; the closed EVALUATE takes the phrase, so the fault is where its parse dies.
    [InlineData("PB3832AK", false, "EVALUATE X WHEN 3 DISPLAY \"T\" WHEN OTHER " + FailsInside + " END-EVALUATE", "OF")]
    [InlineData("PB3832AL", true, "EVALUATE X WHEN 3 DISPLAY \"T\" WHEN OTHER " + FailsInside + " END-EVALUATE", "OF")]
    [InlineData("PB3832Z", false, FailsInside, "OF")]
    [InlineData("PB3832AA", true, FailsInside, "OF")]
    [InlineData("PB3832AB", false, "IF X NOT <> 1 DISPLAY \"C\" END-IF", "<>")]
    [InlineData("PB3832AC", true, "IF X NOT <> 1 DISPLAY \"C\" END-IF", "<>")]
    public void ABlockWhoseStatementFails_IsNotCalledEmpty_AtAnyDepth(string id, bool nested, string statement, string fault)
    {
        var (ok, _, detail) = CompileAndRun(Program(id, (nested ? OpenNest : "") + "           " + statement), 2023);
        Assert.False(ok, $"{id}: a statement that fails inside its block compiled");
        Assert.Equal(new[] { "COBOL0001" }, Codes(detail));
        Assert.Contains($"cannot parse construct near '{fault}'", detail, StringComparison.Ordinal);
    }

    private static string[] Codes(string detail) => [.. Regex.Matches(detail, @"\bCOBOL\d{4}\b").Select(m => m.Value)];

    /// <summary>A program with an indexed file, for the I-O statements' conditional phrases.</summary>
    private const string FileHead =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. {0}.\n       ENVIRONMENT DIVISION.\n"
        + "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + "           SELECT F1 ASSIGN TO \"x.dat\" ORGANIZATION INDEXED ACCESS DYNAMIC RECORD KEY F1-K.\n"
        + "       DATA DIVISION.\n       FILE SECTION.\n       FD F1.\n       01 F1-R.\n          05 F1-K PIC X(4).\n"
        + "       WORKING-STORAGE SECTION.\n       01 X PIC 9(4) VALUE 1.\n       01 S PIC X(8) VALUE SPACES.\n"
        + "       PROCEDURE DIVISION.\n       MAIN-P.\n";

    /// <summary>⛔ EVERY PAIRED CONDITIONAL PHRASE DRAWS ONE DIAGNOSTIC FOR ITS FAULT, AT DEPTH 0 AND INSIDE THE OPEN NEST
    /// (kb/Work PB3832, review R1-2). Each Table 12 phrase family's imperative-statement is unbracketed inside its
    /// phrase (§14.9.2.2 ADD, §14.9.4.2 CALL, §14.9.8.2 COMPUTE, §14.9.30.2 READ, §14.9.43.2 STRING, §14.9.51.2 WRITE), so
    /// writing the phrase obliges the statement (§5.2.6.2). An optional phrase is a decision whose exit ENDS the statement, and ANTLR's SLL
    /// prediction, not knowing what follows the statement, took the exit when the phrase's own alternative died later:
    /// at depth 0 the statement closed before the phrase word, and the report stood there with a period expected, "A
    /// period may be missing" (COBOL0307), while inside the open nest the same fault drew the named COBOL2072. The
    /// report now stands where the closed statement's own parse dies (<c>CobolErrorStrategy.OffendingPosition.CarryForward</c>):
    /// the empty block names its statement, and a failing statement draws the lone statement's COBOL0001 at OF.</summary>
    [Theory]
    [InlineData("PB3832PA", "ADD 1 TO X ON SIZE ERROR {0} END-ADD", "ADD")]
    [InlineData("PB3832PB", "ADD 1 TO X NOT ON SIZE ERROR {0} END-ADD", "ADD")]
    [InlineData("PB3832PC", "COMPUTE X = X + 1 SIZE ERROR {0} END-COMPUTE", "COMPUTE")]
    [InlineData("PB3832PD", "READ F1 NEXT AT END {0} END-READ", "READ")]
    [InlineData("PB3832PE", "READ F1 NEXT NOT AT END {0} END-READ", "READ")]
    [InlineData("PB3832PF", "READ F1 INVALID KEY {0} END-READ", "READ")]
    [InlineData("PB3832PG", "READ F1 NOT INVALID KEY {0} END-READ", "READ")]
    [InlineData("PB3832PH", "WRITE F1-R INVALID KEY {0} END-WRITE", "WRITE")]
    [InlineData("PB3832PI", "STRING \"A\" DELIMITED BY SIZE INTO S ON OVERFLOW {0} END-STRING", "STRING")]
    [InlineData("PB3832PJ", "CALL \"P\" ON EXCEPTION {0} END-CALL", "CALL")]
    [InlineData("PB3832PK", "CALL \"P\" NOT ON EXCEPTION {0} END-CALL", "CALL")]
    [InlineData("PB3832PL", "CALL \"P\" ON OVERFLOW {0} END-CALL", "CALL")]
    public void EveryConditionalPhrase_EmptyOrFailing_DrawsItsOneDiagnostic_AtEveryDepth(string id, string statement, string owner)
    {
        int row = 0;
        foreach (bool nested in new[] { false, true })
        {
            string Compile(string block)
            {
                string programId = $"{id}{++row}";
                string body = (nested ? OpenNest : "") + "           " + string.Format(statement, block);
                var (ok, _, detail) = CompileAndRun(string.Format(FileHead, programId) + body + "\n           STOP RUN.\n", 2023);
                Assert.False(ok, $"{programId}: {body} compiled");
                return detail;
            }
            string empty = Compile("");
            Assert.Equal(new[] { "COBOL2072" }, Codes(empty));
            Assert.Contains($"The {owner} statement's general format", empty, StringComparison.Ordinal);
            string failing = Compile(FailsInside);
            Assert.Equal(new[] { "COBOL0001" }, Codes(failing));
            Assert.Contains("cannot parse construct near 'OF'", failing, StringComparison.Ordinal);
        }
    }

    /// <summary>The r14 repro and the lone IF draw the SAME diagnostic, once: the nested parse used to report the fault
    /// at OF twice (the recovery resumed at the dangling END-IF and met it again), with a false COBOL2072 on the
    /// first (kb/Work PB3832).</summary>
    [Fact]
    public void TheNestedFailure_DrawsTheLoneIfsOneDiagnostic()
    {
        var (_, _, lone) = CompileAndRun(Program("PB3832L1", "           " + FailsInside), 2023);
        var (_, _, nested) = CompileAndRun(Program("PB3832L2", OpenNest + "           " + FailsInside), 2023);
        Assert.Equal(new[] { "COBOL0001" }, Codes(lone));
        Assert.Equal(Codes(lone), Codes(nested));
    }

    /// <summary>⛔ THE COMPLEMENT, so the guard above cannot be satisfied by rejecting the construct outright:
    /// each position written WITH its imperative still compiles and runs, and produces the value the general
    /// rules give it.</summary>
    [Theory]
    [InlineData("PB396K", "           IF X = 1\n               DISPLAY \"ONE\"\n           END-IF", "ONE")]
    [InlineData("PB396L", "           IF X = 9\n               DISPLAY \"NINE\"\n           ELSE\n"
                        + "               DISPLAY \"ONE\"\n           END-IF", "ONE")]
    [InlineData("PB396M", "           EVALUATE X\n               WHEN 1 DISPLAY \"ONE\"\n"
                        + "               WHEN OTHER DISPLAY \"OTHER\"\n           END-EVALUATE", "ONE")]
    [InlineData("PB396N", "           EVALUATE X\n               WHEN 7\n               WHEN 1 DISPLAY \"ONE\"\n"
                        + "           END-EVALUATE", "ONE")]
    [InlineData("PB396O", "           EVALUATE X\n               WHEN 7 DISPLAY \"SEVEN\"\n"
                        + "               WHEN OTHER DISPLAY \"ONE\"\n           END-EVALUATE", "ONE")]
    [InlineData("PB396P", "           PERFORM UNTIL X > 3\n               ADD 1 TO X\n           END-PERFORM\n"
                        + "           DISPLAY \"ONE\"", "ONE")]
    [InlineData("PB396Q", "           MOVE 7 TO R (2)\n           SET I TO 1\n           SEARCH R\n"
                        + "               AT END DISPLAY \"NF\"\n               WHEN R (I) = 7 DISPLAY \"ONE\"\n"
                        + "           END-SEARCH", "ONE")]
    public void TheSamePosition_WrittenWithItsImperative_StillCompilesAndRuns(string id, string body, string expected)
    {
        var (ok, stdout, detail) = CompileAndRun(Program(id, body));
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout);
    }

    // ── §14.5.1: an imperative-statement operand admits no CONDITIONAL statement (kb/Work PB351) ──────────

    /// <summary>⛔ THE OPERAND IS AN IMPERATIVE-STATEMENT IN EVERY PHRASE BUT IF'S. §14.5.1: "Any statement with a
    /// conditional phrase that is not terminated by its explicit scope terminator is a conditional statement", and an
    /// imperative statement may be a conditional statement only "delimited by its explicit scope terminator". The
    /// screen (<c>ProcedureFormatPass</c>) asks it of every <c>statementBlock</c> whose parent is not the IF statement,
    /// so each phrase family below is refused COBOL2796 with no code of its own — the undelimited inner statement
    /// is an IF in some rows and a conditional ADD in others, because the classification is Table 12's, not IF's.</summary>
    [Theory]
    [InlineData("PB351A", "           ADD 1 TO X ON SIZE ERROR IF X = 1 DISPLAY \"A\"\n           END-ADD")]
    [InlineData("PB351B", "           EVALUATE X\n               WHEN 1 ADD 1 TO X ON SIZE ERROR DISPLAY \"S\"\n"
                        + "               WHEN OTHER DISPLAY \"O\"\n           END-EVALUATE")]
    [InlineData("PB351C", "           EVALUATE X\n               WHEN 7 DISPLAY \"S\"\n"
                        + "               WHEN OTHER IF X = 1 DISPLAY \"O\"\n           END-EVALUATE")]
    [InlineData("PB351D", "           PERFORM 2 TIMES\n               IF X = 1 DISPLAY \"P\"\n           END-PERFORM")]
    [InlineData("PB351E", "           SET I TO 1\n           SEARCH R\n               AT END IF X = 1 DISPLAY \"NF\"\n"
                        + "               WHEN R (I) = 7 DISPLAY \"F\"\n           END-SEARCH")]
    public void AnUndelimitedConditionalStatement_AsAnImperativeOperand_IsRejected(string id, string body)
    {
        var (ok, _, detail) = CompileAndRun(Program(id, body));
        Assert.False(ok, $"{id} compiled: a conditional statement was accepted as an imperative-statement (§14.5.1).");
        Assert.Contains("COBOL2796", detail, StringComparison.Ordinal);
    }

    /// <summary>The complement: the same inner statements DELIMITED by their END- terminators are imperative
    /// statements and compile, and IF's statement-1 may END in an undelimited conditional statement (§14.9.19.3 SR1:
    /// "either one or more imperative statements or a conditional statement optionally preceded by one or more
    /// imperative statements"). X is 1, so ADD 9 TO X (PIC 9(4)) does not overflow and EVALUATE takes WHEN 1.</summary>
    [Theory]
    [InlineData("PB351K", "           EVALUATE X\n               WHEN 1 ADD 9 TO X ON SIZE ERROR DISPLAY \"S\" END-ADD\n"
                        + "                      DISPLAY \"ONE\"\n           END-EVALUATE", "ONE")]
    [InlineData("PB351L", "           IF X = 1\n               DISPLAY \"ONE\"\n"
                        + "               ADD 1 TO X ON SIZE ERROR DISPLAY \"S\"\n           END-IF", "ONE")]
    public void TheDelimitedForm_AndIfsOwnStatementOperand_StillCompile(string id, string body, string expected)
    {
        var (ok, stdout, detail) = CompileAndRun(Program(id, body));
        Assert.True(ok, detail);
        Assert.Equal(expected, stdout);
    }

    // ── §14.9.13.4 GR3: the selection subject is evaluated at the BEGINNING of the statement ─────────────

    /// <summary>⛔ GR3 IS AN OBLIGATION OF THE STATEMENT, NOT OF WHICHEVER ARM READS THE SUBJECT. "At the
    /// beginning of the execution of the EVALUATE statement, each selection subject is evaluated and assigned a
    /// value, a range of values, or a truth value" (ISO §14.9.13.4 GR3). An <c>ANY</c> object makes the pair true
    /// without consulting the subject (GR4 a) 1.), so before kb/Work PB396 an EVALUATE whose objects were all ANY
    /// bound the subject lazily — which is to say never — and the out-of-range reference it names raised nothing.
    /// The A/B pair differs ONLY in the object, so the subject's evaluation is the single variable.</summary>
    [Fact]
    public void EvaluateSubject_IsEvaluated_EvenWhenNoArmReadsIt()
    {
        const string body = "           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON\n"
            + "           EVALUATE R (9)\n               WHEN ANY DISPLAY \"ARM\"\n           END-EVALUATE\n"
            + "           DISPLAY \"AFTER\"";
        var (ok, stdout, detail) = CompileAndRun(Program("PB396GR3A", body), dialectLevel: 2023);
        Assert.False(ok, $"the subscript 9 on a 3-occurrence table was never evaluated: {stdout}");
        Assert.Contains("EC-BOUND-SUBSCRIPT", detail, StringComparison.Ordinal);
    }

    /// <summary>The B twin — the identical statement with an object that DOES read the subject — so the test
    /// above cannot pass because the reference is rejected for some unrelated reason.</summary>
    [Fact]
    public void EvaluateSubject_IsEvaluated_WhenAnArmReadsIt()
    {
        const string body = "           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON\n"
            + "           EVALUATE R (9)\n               WHEN 5 DISPLAY \"ARM\"\n           END-EVALUATE\n"
            + "           DISPLAY \"AFTER\"";
        var (ok, _, detail) = CompileAndRun(Program("PB396GR3B", body), dialectLevel: 2023);
        Assert.False(ok);
        Assert.Contains("EC-BOUND-SUBSCRIPT", detail, StringComparison.Ordinal);
    }

    /// <summary>And the control: the SAME statement with a subscript in range runs, so the two assertions above
    /// are about the bound and not about the shape.</summary>
    [Fact]
    public void EvaluateSubject_InRange_RunsNormally()
    {
        const string body = "           >>TURN EC-BOUND-SUBSCRIPT CHECKING ON\n"
            + "           EVALUATE R (2)\n               WHEN ANY DISPLAY \"ARM\"\n           END-EVALUATE\n"
            + "           DISPLAY \"AFTER\"";
        var (ok, stdout, detail) = CompileAndRun(Program("PB396GR3C", body), dialectLevel: 2023);
        Assert.True(ok, detail);
        Assert.Equal("ARM\r\nAFTER", stdout);
    }
}
