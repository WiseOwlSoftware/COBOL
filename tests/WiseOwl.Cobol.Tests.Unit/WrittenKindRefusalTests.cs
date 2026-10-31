// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Generated;
using WiseOwl.Cobol.Frontend.Parsing;
using WiseOwl.Cobol.Frontend.Preprocessor;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ THE KIND WRITTEN, AGAINST THE KINDS THE PARSE EXPECTS THERE (kb/Work PB3428, PB3861): <c>CobolErrorStrategy</c>
/// hint 0i, worded by <c>AdmittedKinds</c>. A context word a text predicate refuses inside a clause it has entered
/// (MEMORY SIZE's unit, DYNAMIC LENGTH's SHORT and PREFIXED), an operand left out (SEGMENT-LIMIT's integer, MOVE's TO), a
/// literal of a category the position does not take (SET IX TO "A", whose Format 1 takes a numeric literal or ZERO,
/// ISO §14.9.39.2 with §8.8.1.1) and a literal where only a user-defined word may stand (CALL … RETURNING 5) each draw
/// ONE COBOL5506 naming what is expected there. The words a predicate tests are read by running it, through the parser
/// base's one word funnel, so no table lists them.
/// <para>The controls pin what the arm must NOT claim: a level-number after a forgotten period stays the missing-period
/// reading, a word refused at a closed format's residue stays the format's own refusal (COBOL1970), a level-number
/// before the FD that would own it is no literal operand, and the words the predicate accepts still compile.</para>
/// </summary>
public sealed class WrittenKindRefusalTests
{
    private const string Head = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB3428U.\n";
    private const string Proc = "       PROCEDURE DIVISION.\n           STOP RUN.\n";
    private const string Config = "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n";
    private const string Table = "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-N PIC 9.\n"
        + "       01 W-X PIC X(4).\n       01 TB.\n          05 TE PIC X OCCURS 4 INDEXED BY IX.\n";
    private const string Files = "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + "           SELECT F ASSIGN TO \"pb3428u.dat\".\n       DATA DIVISION.\n       FILE SECTION.\n       FD F.\n"
        + "       01 R PIC X(4).\n";

    [Theory]
    // A word a text predicate refuses inside a clause it has entered: the words the predicate tests are named.
    [InlineData("memory-size-unit", 85, Config + "       OBJECT-COMPUTER. XYZ MEMORY SIZE 12 FOO.\n" + Proc,
        "COBOL5506: cannot parse construct near 'FOO'. The word 'FOO' is not expected here: expected CHARACTERS, MODULES or WORDS")]
    [InlineData("dynamic-length-prefix", 2023, Config + "       SPECIAL-NAMES.\n           DYNAMIC LENGTH STRUCTURE DL IS SIGNED FOO.\n" + Proc,
        "COBOL5506: cannot parse construct near 'FOO'. The word 'FOO' is not expected here: expected PREFIXED or SHORT")]
    // An operand left out: the period stands where the parse expects only an integer literal.
    [InlineData("segment-limit-integer", 85, Config + "       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS.\n" + Proc,
        "COBOL5506: cannot parse construct near '.'. The separator period is not expected here: expected an integer literal")]
    [InlineData("move-to", 2023, Table + "       PROCEDURE DIVISION.\n           MOVE 1 W-N.\n           STOP RUN.\n",
        "COBOL5506: missing token before 'W-N'. The word 'W-N' is not expected here: expected TO")]
    [InlineData("open-mode", 2023, Files + "       PROCEDURE DIVISION.\n           OPEN F.\n           STOP RUN.\n",
        "COBOL5506: unexpected 'F'. The word 'F' is not expected here: expected EXTEND, I-O, INPUT or OUTPUT")]
    // A literal of a category the position does not take, and a literal where no literal is expected. The figurative
    // ZERO is named wherever the parse expects the arithmetic ZERO_ARITH, because a ZERO written there parses: beside an
    // arithmetic operator the token rewriter makes it ZERO_ARITH, and after SET … TO alone the parse does
    // (ZeroSignReading, kb/Work PB4120). The theory below writes the ZERO and requires it to parse.
    [InlineData("set-index-alphanumeric", 2023, Table + "       PROCEDURE DIVISION.\n           SET IX TO \"A\".\n           STOP RUN.\n",
        "COBOL5506: cannot parse construct near '\"A\"'. The alphanumeric literal \"A\" is not expected here: of the literals, "
        + "only a numeric literal or the figurative constant ZERO is expected at this position")]
    [InlineData("set-up-by-alphanumeric", 2023, Table + "       PROCEDURE DIVISION.\n           SET IX UP BY \"A\".\n           STOP RUN.\n",
        "COBOL5506: unexpected '\"A\"'. The alphanumeric literal \"A\" is not expected here: of the literals, only a numeric "
        + "literal or the figurative constant ZERO is expected at this position")]
    [InlineData("compute-operand-alphanumeric", 2023, Table + "       PROCEDURE DIVISION.\n           COMPUTE W-N = 1 + \"A\".\n           STOP RUN.\n",
        "COBOL5506: cannot parse construct near '\"A\"'. The alphanumeric literal \"A\" is not expected here: of the literals, "
        + "only a numeric literal or the figurative constant ZERO is expected at this position")]
    [InlineData("set-left-operand-alphanumeric", 2023, Table + "       PROCEDURE DIVISION.\n           SET IX TO \"A\" + 1.\n           STOP RUN.\n",
        "COBOL5506: cannot parse construct near '\"A\"'. The alphanumeric literal \"A\" is not expected here: of the literals, "
        + "only a numeric literal or the figurative constant ZERO is expected at this position")]
    [InlineData("call-returning-literal", 2023, Table + "       PROCEDURE DIVISION.\n           CALL \"P\" RETURNING 5.\n           STOP RUN.\n",
        "COBOL5506: unexpected '5'. The integer literal 5 is not expected here: no literal is expected at this position")]
    // ⛔ A VERB SHARED WITH ANOTHER CLAUSE IS NO STATEMENT POSITION (kb/Work PB4201). DISPLAY is a statement's opening word
    // AND a USAGE keyword, so the one-token overlap made `USAGE 5` an "open" position and left it the bare COBOL0001; the
    // usage keywords the edition has (ISO §13.18.60.2: [USAGE IS] followed by the usage) are named instead, the 2002
    // words (BIT, NATIONAL, OBJECT, ...) from 2002 and the 2014 floating-point words from 2014.
    [InlineData("usage-literal-85", 85, "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X USAGE 5.\n" + Proc,
        "COBOL5506: unexpected '5'. The integer literal 5 is not expected here: expected BINARY, COMP, COMP-1, COMP-2, COMP-3, "
        + "COMP-4, COMP-5, COMPUTATIONAL, COMPUTATIONAL-1, COMPUTATIONAL-2, COMPUTATIONAL-3, COMPUTATIONAL-4, "
        + "COMPUTATIONAL-5, DISPLAY, INDEX, IS, PACKED-DECIMAL or POINTER (ISO §5.2.1).")]
    [InlineData("usage-literal-2023", 2023, "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X USAGE 5.\n" + Proc,
        "COBOL5506: unexpected '5'. The integer literal 5 is not expected here: expected BINARY, BINARY-CHAR, BINARY-DOUBLE, "
        + "BINARY-LONG, BINARY-SHORT, BIT, COMP, COMP-1, COMP-2, COMP-3, COMP-4, COMP-5, COMPUTATIONAL, COMPUTATIONAL-1, "
        + "COMPUTATIONAL-2, COMPUTATIONAL-3, COMPUTATIONAL-4, COMPUTATIONAL-5, DISPLAY, FLOAT-BINARY-128, FLOAT-BINARY-32, "
        + "FLOAT-BINARY-64, FLOAT-DECIMAL-16, FLOAT-DECIMAL-34, FLOAT-EXTENDED, FLOAT-LONG, FLOAT-SHORT, FUNCTION-POINTER, "
        + "INDEX, IS, MESSAGE-TAG, NATIONAL, OBJECT, PACKED-DECIMAL, POINTER or PROGRAM-POINTER (ISO §5.2.1).")]
    public void AWrittenKindThePositionDoesNotExpect_IsNamedOnce(string name, int edition, string source, string expected)
    {
        string text = Head + source;
        var errors = Compile(text, edition);
        Assert.True(errors.Count == 1, $"[{name}] expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains(expected, errors[0], StringComparison.Ordinal);
        // ⛔ A NAMED ELEMENT IS ONE THE PARSE TAKES THERE (review R1-1): where the refusal names the figurative constant
        // ZERO, the same statement with ZERO written for the refused literal parses.
        if (expected.Contains("the figurative constant ZERO", StringComparison.Ordinal))
        {
            var rewritten = CompileErrors(text.Replace("\"A\"", "ZERO", StringComparison.Ordinal), edition);
            Assert.True(!rewritten.Any(e => e.Contains("COBOL0001", StringComparison.Ordinal)
                                            || e.Contains("COBOL5506", StringComparison.Ordinal)),
                $"[{name}] the refusal names ZERO, and ZERO written there does not parse:\n{string.Join("\n", rewritten)}");
        }
    }

    private const string Options = "       OPTIONS.\n           ARITHMETIC IS FOO.\n       DATA DIVISION.\n" + Proc;
    private const string RoundedMode = Table + "       PROCEDURE DIVISION.\n           ADD 1 TO W-N ROUNDED MODE IS FOO.\n           STOP RUN.\n";

    private const string UseDeclarative = Head + Files
        + "       PROCEDURE DIVISION.\n       DECLARATIVES.\n       H-SECT SECTION.\n           USE PROCEDURE ON F.\n"
        + "       H-PARA.\n           DISPLAY \"H\".\n       END DECLARATIVES.\n       MAIN-SECT SECTION.\n       MAIN.\n"
        + "           STOP RUN.\n";

    /// <summary>⛔ THE WORDS NAMED ARE THE ONES THE TARGETED EDITION HAS (kb/Work PB4174). The parse is the superset of
    /// the four editions, so its expected set holds words ISO §8.9 has not reserved at the edition — and §8.3.2.1 1)
    /// ("Reserved words shall not be used as user-defined words or system-names") makes such a word a user-defined
    /// one there. The 2002 boolean operators B-AND, B-OR and B-XOR, the 2023 shift operators (Annex E.2 3), PROTOTYPE
    /// (2002) and EC and EO (2002) are named from the edition that reserves them and not before.</summary>
    [Theory]
    [InlineData("paren-85", 85, Head + Table + "       PROCEDURE DIVISION.\n           COMPUTE W-N = (W-N + 1.\n           STOP RUN.\n",
        "expected '**', ')', '+', '-', '*' or '/' (ISO §5.2.1)", "B-")]
    [InlineData("paren-2002", 2002, Head + Table + "       PROCEDURE DIVISION.\n           COMPUTE W-N = (W-N + 1.\n           STOP RUN.\n",
        "expected B-AND, B-OR, B-XOR, '**', ')', '+', '-', '*' or '/' (ISO §5.2.1)", "B-SHIFT")]
    [InlineData("paren-2014", 2014, Head + Table + "       PROCEDURE DIVISION.\n           COMPUTE W-N = (W-N + 1.\n           STOP RUN.\n",
        "expected B-AND, B-OR, B-XOR, '**', ')', '+', '-', '*' or '/' (ISO §5.2.1)", "B-SHIFT")]
    [InlineData("paren-2023", 2023, Head + Table + "       PROCEDURE DIVISION.\n           COMPUTE W-N = (W-N + 1.\n           STOP RUN.\n",
        "expected B-AND, B-OR, B-SHIFT-L, B-SHIFT-LC, B-SHIFT-R, B-SHIFT-RC, B-XOR, '**', ')', '+', '-', '*' or '/' (ISO §5.2.1)", null)]
    [InlineData("program-id-attribute-85", 85, "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4174G IS GLOBAL.\n" + Proc,
        "expected COMMON or INITIAL (ISO §5.2.1)", "PROTOTYPE")]   // RECURSIVE is in the 2002 list of §8.10, PROTOTYPE a 2002 §8.9 word
    [InlineData("program-id-attribute-2002", 2002, "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB4174G IS GLOBAL.\n" + Proc,
        "expected COMMON, INITIAL, PROTOTYPE or RECURSIVE (ISO §5.2.1)", null)]
    [InlineData("use-85", 85, UseDeclarative, "expected AFTER, BEFORE, ERROR, EXCEPTION", "EC, EO")]
    [InlineData("use-2002", 2002, UseDeclarative, "expected AFTER, BEFORE, EC, EO, ERROR, EXCEPTION", null)]
    // The §8.10 context-sensitive words follow the same rule, from the edition of their first listing
    // (ContextSensitiveWordEntry.Introduced): STANDARD-BINARY and STANDARD-DECIMAL are 2014's, NATIVE and STANDARD 2002's
    // (ISO §8.10, whose 2002 list this repository records, and the 2014 words that Annex E.3.3 item 13 does not call new).
    [InlineData("arithmetic-2002", 2002, Head + Options, "expected NATIVE or STANDARD (ISO §5.2.1)", "STANDARD-")]
    [InlineData("arithmetic-2014", 2014, Head + Options, "expected NATIVE, STANDARD, STANDARD-BINARY or STANDARD-DECIMAL (ISO §5.2.1)", null)]
    [InlineData("arithmetic-2023", 2023, Head + Options, "expected NATIVE, STANDARD, STANDARD-BINARY or STANDARD-DECIMAL (ISO §5.2.1)", null)]
    // ROUNDED MODE is a 2014 phrase: its mode names are named from 2014 (§14.7.4; the construct registry's rounded-mode-is-2014).
    [InlineData("rounded-mode-2014", 2014, Head + RoundedMode,
        "expected AWAY-FROM-ZERO, NEAREST-AWAY-FROM-ZERO, NEAREST-EVEN, NEAREST-TOWARD-ZERO, PROHIBITED, TOWARD-GREATER, TOWARD-LESSER or TRUNCATION (ISO §5.2.1)", null)]
    public void TheWordsNamedAreTheOnesTheEditionHas(string name, int edition, string source, string expected, string? absent)
    {
        var errors = Compile(source, edition).Where(e => e.Contains("COBOL5506", StringComparison.Ordinal)).ToList();
        Assert.True(errors.Count == 1, $"[{name}] expected exactly one COBOL5506, got:\n{string.Join("\n", errors)}");
        Assert.Contains(expected, errors[0], StringComparison.Ordinal);
        if (absent is not null)
        {
            string sentence = errors[0][errors[0].IndexOf("expected ", StringComparison.Ordinal)..];
            Assert.DoesNotContain(absent, sentence, StringComparison.Ordinal);
        }
    }

    /// <summary>The rule itself (<c>ReservedWords.AdmitsAsKeyword</c>): false only for a word a table proves a LATER
    /// edition introduced (the §8.9 flags, or a §8.10 row's <c>Introduced</c>); true for a reserved word, for one an older
    /// edition reserved and this one dropped (WORDS at 2023, COBOL and PHYSICAL, which 2023 formats still underline), for
    /// one in neither table and for one whose §8.9 row may not reject.</summary>
    [Theory]
    [InlineData("B-AND", 85, false)]
    [InlineData("B-AND", 2002, true)]
    [InlineData("B-SHIFT-L", 2014, false)]
    [InlineData("B-SHIFT-L", 2023, true)]
    [InlineData("PROTOTYPE", 85, false)]
    [InlineData("PROTOTYPE", 2023, true)]
    [InlineData("WORDS", 2023, true)]
    [InlineData("COBOL", 2023, true)]
    [InlineData("PHYSICAL", 2023, true)]
    [InlineData("PHYSICAL", 85, false)]
    [InlineData("NO-SUCH-WORD", 85, true)]
    // The §8.10 context-sensitive words, from the edition of their first listing (ContextSensitiveWordEntry.Introduced):
    // 2002 from the 2002 list (AUTOMATIC, SECONDS), 2014 for the rest of the table that Annex E.3.3 item 13 does not
    // call new (the ROUNDED MODE names, STANDARD-BINARY), 2023 for item 13's additions (BYTES, BACKWARD, HEX).
    [InlineData("AUTOMATIC", 85, false)]
    [InlineData("AUTOMATIC", 2002, true)]
    [InlineData("SECONDS", 85, false)]
    [InlineData("SECONDS", 2002, true)]
    [InlineData("AWAY-FROM-ZERO", 85, false)]
    [InlineData("AWAY-FROM-ZERO", 2002, false)]
    [InlineData("AWAY-FROM-ZERO", 2014, true)]
    [InlineData("STANDARD-BINARY", 2002, false)]
    [InlineData("STANDARD-BINARY", 2014, true)]
    [InlineData("BYTES", 2014, false)]
    [InlineData("BYTES", 2023, true)]
    [InlineData("BACKWARD", 2014, false)]
    [InlineData("HEX", 2023, true)]
    // A word an older edition RESERVED outright is §8.9's, whatever §8.10 says: MULTIPLE is a 1985 reserved word.
    [InlineData("MULTIPLE", 85, true)]
    public void AKeywordIsNamedUnlessALaterEditionIntroducedIt(string word, int edition, bool expected)
        => Assert.Equal(expected, WiseOwl.Cobol.Editions.ReservedWords.AdmitsAsKeyword(word, edition));

    /// <summary>The controls: each keeps the diagnosis it had, and none draws COBOL5506.</summary>
    [Theory]
    [InlineData("residue-word", 85, Config + "       OBJECT-COMPUTER. XYZ FOO IS 12.\n" + Proc, "COBOL1970")]
    [InlineData("level-number-after-missing-period", 2023,
        "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X\n       05 W-Y PIC X.\n" + Proc, "COBOL0307")]
    [InlineData("level-number-before-its-fd", 2023, "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n"
        + "       FILE-CONTROL.\n           SELECT F ASSIGN TO \"pb3428u.dat\".\n       DATA DIVISION.\n       FILE SECTION.\n"
        + "       01 W-LOOSE PIC X(5).\n       FD F.\n       01 R PIC X(5).\n" + Proc, "COBOL0001")]
    // A position where a statement may begin expects every statement's opening word, which the imperative-statement arms
    // own: a literal there stays the bare COBOL0001, never a list of fifty verbs (kb/Work PB4201).
    // ROUNDED MODE IS is a 2014 phrase, so below 2014 no mode name is a keyword and the position has nothing to name
    // (kb/Work PB4174): the bare COBOL0001, never the 2014 names sent to a COBOL-85 or 2002 reader.
    [InlineData("rounded-mode-85", 85, RoundedMode, "COBOL0001")]
    [InlineData("rounded-mode-2002", 2002, RoundedMode, "COBOL0001")]
    [InlineData("literal-at-a-statement-start", 2023,
        Table + "       PROCEDURE DIVISION.\n           IF W-N = 1 THEN 5 END-IF.\n           STOP RUN.\n", "COBOL0001")]
    public void APositionTheArmDoesNotName_KeepsItsDiagnosis(string name, int edition, string body, string code)
    {
        var errors = Compile(Head + body, edition);
        Assert.Contains(errors, e => e.Contains($"error {code}: ", StringComparison.Ordinal));
        Assert.DoesNotContain(errors, e => e.Contains("COBOL5506", StringComparison.Ordinal));
        Assert.True(errors.Count >= 1, name);
    }

    /// <summary>The words the predicate accepts still parse: MEMORY SIZE's three units at COBOL-85.</summary>
    [Theory]
    [InlineData("WORDS")]
    [InlineData("MODULES")]
    [InlineData("CHARACTERS")]
    public void TheWordsThePredicateTests_StillParse(string unit)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB3428_");
        try
        {
            string src = Path.Combine(dir, "pb3428u.cob");
            File.WriteAllText(src, Head + Config + $"       OBJECT-COMPUTER. XYZ MEMORY SIZE 12 {unit}.\n" + Proc);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb3428u.dll"), DialectLevel: 85, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            Assert.True(r.Success, $"MEMORY SIZE 12 {unit} is the '85 clause; got:\n{string.Join("\n", r.Errors)}");
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    /// <summary>⛔ EVERY WHOLE-LITERAL TOKEN HAS ITS §8.3.3.1 CATEGORY (kb/Work PB3861). The refusal names a literal by
    /// <see cref="LiteralTokens.CategoryOf"/>; a literal token the grammar gains tomorrow (FIRST(residueLiteral) and the
    /// unsigned integer, read from the ATN) is red here until it is named there.</summary>
    [Fact]
    public void EveryWholeLiteralToken_HasACategory()
    {
        var unnamed = LiteralTokens.Whole.ToIntegerList()
            .Where(t => LiteralTokens.CategoryOf(t) is null)
            .Select(t => CobolLexer.DefaultVocabulary.GetSymbolicName(t))
            .ToList();
        Assert.True(unnamed.Count == 0, $"literal tokens with no §8.3.3.1 category: {string.Join(", ", unnamed)}");
    }

    private static IReadOnlyList<string> Compile(string source, int edition)
    {
        var errors = CompileErrors(source, edition);
        Assert.False(errors.Count == 0, "the source is non-conforming and must be rejected");
        return errors;
    }

    private static IReadOnlyList<string> CompileErrors(string source, int edition)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB3428_");
        try
        {
            string src = Path.Combine(dir, "pb3428u.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb3428u.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            return r.Success ? [] : r.Errors;
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
