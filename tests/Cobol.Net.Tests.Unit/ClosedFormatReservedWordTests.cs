// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A RESERVED WORD IN A CLOSED GENERAL FORMAT IS REFUSED BY NAME (kb/Work PB2533). Every closed format ends its
/// alternative list in the <c>unrecognizedClause</c> error production, which <c>ClosedFormatPass</c> refuses naming the
/// format (COBOLNET1941 / 1970 / 1971) — but the production never begins at a reserved word, so one the format does
/// not print (<c>SD SF GLOBAL.</c>) never entered it and drew a bare parse error. <c>CobolErrorStrategy</c> now finds
/// the production's position in the ATN and reports the pass's own refusal, from the same <c>ClosedFormats</c> row.
/// A LITERAL is held here too (kb/Work PB3373, <see cref="LiteralResidue"/>): the production opens at a one-token
/// literal, and the parse layer refuses the unsigned integer it does not open with.
/// <para>One case per closed format whose position the parser reaches, each asserting the code, the sentence and that
/// it is the ONLY diagnostic (the recovery consumes the residue). The controls pin what the arm must NOT claim: a
/// missing separator period before the next construct, a paragraph header written out of order, and a clause of the
/// format that is malformed rather than foreign.</para>
/// </summary>
public sealed class ClosedFormatReservedWordTests
{
    private const string Head = "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. PB2533U.\n";
    private const string Select = "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n"
        + "           SELECT SF ASSIGN TO \"pb2533u.dat\".\n";
    private const string Proc = "       PROCEDURE DIVISION.\n           STOP RUN.\n";

    public static TheoryData<string, string, string, string> ReservedWordResidue() => new()
    {
        // §13.16.2 Format 1 — after a complete entry, so the clause list has already ended when BLOCK fails.
        { "data-entry", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X BLOCK CONTAINS 10.\n" + Proc,
          "COBOLNET1941", "'BLOCK' is not a clause of the data description entry for 'W-X'" },
        // §13.16.2 again, on a level-88 entry: its clauses are the same alternative list.
        { "condition-entry", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X.\n          88 W-C VALUE \"A\" BLOCK.\n" + Proc,
          "COBOLNET1941", "'BLOCK' is not a clause of the data description entry for 'W-C'" },
        // §13.4.5.2 — the FD.
        { "fd", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF DUPLICATES.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'DUPLICATES' is not a clause of the file description entry for 'SF'" },
        // §13.4.6.2 — the SD, with no clause written (the note's repro) ...
        { "sd-global", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF GLOBAL.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the sort-merge file description entry for 'SF'" },
        // ... and after a complete RECORD clause, where the list may end and the base Sync let the word pass.
        { "sd-after-record", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF RECORD 80 EXTERNAL.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'EXTERNAL' is not a clause of the sort-merge file description entry for 'SF'" },
        // A multi-word residue is ONE refusal: the recovery consumes it to the period.
        { "sd-block-contains", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF BLOCK CONTAINS 10 RECORDS.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'BLOCK' is not a clause of the sort-merge file description entry for 'SF'" },
        // §12.4.5.1 — the file control entry.
        { "select", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\" GLOBAL.\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the file control entry for 'SF'" },
        // §12.4.6.2 — the I-O-CONTROL paragraph.
        { "io-control", Head + Select + "       I-O-CONTROL.\n           GLOBAL.\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the I-O-CONTROL paragraph" },
        // §12.3.7.2 — the SPECIAL-NAMES paragraph: the INNERMOST open list names it, not the configuration section's.
        { "special-names", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           GLOBAL.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the SPECIAL-NAMES paragraph" },
        // §12.3.6.2 — the OBJECT-COMPUTER paragraph, whose clauses may follow its period with no computer-name.
        // (SOURCE-COMPUTER prints nothing but the name there, so `SOURCE-COMPUTER. GLOBAL.` reads as a configuration
        // section paragraph — the configuration-section case below.)
        { "object-computer", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. GLOBAL.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the OBJECT-COMPUTER paragraph" },
        // §12.3.5.2 / §12.3.6.2 AFTER the computer-name (kb/Work PB3370): XYZ could be the name or an unrecognized
        // configuration paragraph, both readings fail at GLOBAL, and the error-time prediction used to take the
        // configuration section's (a bare COBOL0307). The paragraphs' superset-parsed second period makes the name win.
        { "source-computer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ GLOBAL.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ GLOBAL.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the OBJECT-COMPUTER paragraph" },
        // ... and after an unrecognized word run, where no list loop of the format's own is left to Sync: the residue
        // continues inside the production (CobolErrorStrategy reads the parser's own call of it).
        { "source-computer-after-residue", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ WIBBLE GLOBAL.\n" + Proc,
          "COBOLNET1970", "'GLOBAL' is not a clause of the SOURCE-COMPUTER paragraph" },
        // §12.3.2 — the configuration section's PARAGRAPH list.
        { "configuration-section", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       GLOBAL.\n" + Proc,
          "COBOLNET1971", "'GLOBAL' is not a paragraph of the CONFIGURATION SECTION" },
        { "configuration-section-after-residue", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       WIBBLE GLOBAL.\n" + Proc,
          "COBOLNET1971", "'GLOBAL' is not a paragraph of the CONFIGURATION SECTION" },
        // §11.2.1 — the identification division's PARAGRAPH list.
        { "identification", Head + "       GLOBAL.\n" + Proc,
          "COBOLNET1971", "'GLOBAL' is not a paragraph of the IDENTIFICATION DIVISION" },
        { "identification-after-residue", Head + "       WIBBLE GLOBAL.\n" + Proc,
          "COBOLNET1971", "'GLOBAL' is not a paragraph of the IDENTIFICATION DIVISION" },
        // A FIGURATIVE CONSTANT after the computer-name is a reserved word too (kb/Work PB3373): the word run's tail used
        // to be the whole `literal` rule, which read `XYZ ZERO` as one run and XYZ as the clause.
        { "object-computer-figurative-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ ZERO.\n" + Proc,
          "COBOLNET1970", "'ZERO' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "source-computer-figurative-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ ZERO.\n" + Proc,
          "COBOLNET1970", "'ZERO' is not a clause of the SOURCE-COMPUTER paragraph" },
    };

    /// <summary>⛔ A LITERAL IN A CLOSED GENERAL FORMAT IS REFUSED BY NAME, IN ITS OWN FORMAT (kb/Work PB3373). The
    /// error production opens at a one-token or sign-led literal as it does at a word, so the literal is the run
    /// ClosedFormatPass quotes; after a computer-name the name's reading wins, where the run used to be readable only
    /// as XYZ's (the note's repro: <c>SOURCE-COMPUTER. XYZ "LIT".</c> reported XYZ as a CONFIGURATION SECTION
    /// paragraph). The unsigned integer, which the production never opens with, is refused by the parse layer from the
    /// same row. One case per format, a literal of each class among them, each the ONLY diagnostic.</summary>
    public static TheoryData<string, string, string, string> LiteralResidue() => new()
    {
        { "source-computer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ \"LIT\".\n" + Proc,
          "COBOLNET1970", "'\"LIT\"' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "source-computer-decimal-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ 1.5.\n" + Proc,
          "COBOLNET1970", "'1.5' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ \"LIT\".\n" + Proc,
          "COBOLNET1970", "'\"LIT\"' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-without-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. N\"LIT\".\n" + Proc,
          "COBOLNET1970", "'N\"LIT\"' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-integer", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. 12.\n" + Proc,
          "COBOLNET1970", "'12' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "special-names", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA B\"1\".\n" + Proc,
          "COBOLNET1970", "'B\"1\"' is not a clause of the SPECIAL-NAMES paragraph" },
        { "configuration-section", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       \"LIT\".\n" + Proc,
          "COBOLNET1971", "'\"LIT\"' is not a paragraph of the CONFIGURATION SECTION" },
        { "identification", Head + "       X\"41\".\n" + Proc,
          "COBOLNET1971", "'X\"41\"' is not a paragraph of the IDENTIFICATION DIVISION" },
        { "select", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\"\n"
            + "               ORGANIZATION IS SEQUENTIAL \"LIT\".\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'\"LIT\"' is not a clause of the file control entry for 'SF'" },
        { "io-control", Head + Select + "       I-O-CONTROL.\n           \"LIT\".\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'\"LIT\"' is not a clause of the I-O-CONTROL paragraph" },
        { "fd", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF \"LIT\".\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'\"LIT\"' is not a clause of the file description entry for 'SF'" },
        { "sd", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF \"LIT\".\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'\"LIT\"' is not a clause of the sort-merge file description entry for 'SF'" },
        { "data-entry", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X \"A\".\n" + Proc,
          "COBOLNET1941", "'\"A\"' is not a clause of the data description entry for 'W-X'" },
        // A LITERAL OF MORE THAN ONE TOKEN is a literal too (kb/Work PB3373, review finding R1-1): a sign-led numeric
        // literal (ISO §8.3.3.3.2 2), MINUS INTEGERLIT outside the list regions), a concatenation expression (§8.8.3.3
        // 3)) and a DECIMAL-POINT IS COMMA literal. The run opens at a sign-led one and continues through all three,
        // where it used to end at the sign, the `&` or the comma with a bare parse error and a false missing period.
        { "source-computer-signed-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ +1.5.\n" + Proc,
          "COBOLNET1970", "'+1.5' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "source-computer-concatenation-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ \"A\" & \"B\".\n" + Proc,
          "COBOLNET1970", "'\"A\"' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-signed-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ -1.\n" + Proc,
          "COBOLNET1970", "'-1' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-signed-without-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. -1.\n" + Proc,
          "COBOLNET1970", "'-1' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-signed-in-run", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ FOO -1 \"A\" & ZERO.\n" + Proc,
          "COBOLNET1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "data-entry-signed-opening", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X -1.\n" + Proc,
          "COBOLNET1941", "'-1' is not a clause of the data description entry for 'W-X'" },
        { "data-entry-signed-in-run", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO -1.\n" + Proc,
          "COBOLNET1941", "'FOO' is not a clause of the data description entry for 'W-X'" },
        { "data-entry-concatenation-in-run", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO \"A\" & \"B\".\n" + Proc,
          "COBOLNET1941", "'FOO' is not a clause of the data description entry for 'W-X'" },
        { "data-entry-comma-decimal-in-run", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA.\n"
            + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO 1,5.\n" + Proc,
          "COBOLNET1941", "'FOO' is not a clause of the data description entry for 'W-X'" },
        { "fd-signed-in-run", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF FOO +2.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'FOO' is not a clause of the file description entry for 'SF'" },
        { "select-signed-in-run", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\"\n"
            + "               ORGANIZATION IS SEQUENTIAL FOO +2.\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOLNET1970", "'FOO' is not a clause of the file control entry for 'SF'" },
    };

    /// <summary>A sign-led literal after a clause that takes another word (kb/Work PB3373, review finding R1-1):
    /// <c>FOO BAR</c> is an implementor-name clause's shape and <c>ASSIGN TO "f.dat" FOO</c> an ASSIGN operand list, so
    /// the clause keeps them (loop continuation, the lower alternative, as for a one-token literal) and the run opens at
    /// the sign-led literal, which is refused by name — never the bare parse error with a false missing period it drew
    /// while the run could not hold a sign.</summary>
    [Theory]
    [InlineData("special-names", "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           FOO BAR -1.\n",
        "COBOLNET1970: '-1' is not a clause of the SPECIAL-NAMES paragraph")]
    [InlineData("select", "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\" FOO +2.\n"
        + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n",
        "COBOLNET1970: '+2' is not a clause of the file control entry for 'SF'")]
    public void SignedLiteralAfterAWordTakingClause_IsRefusedByName(string name, string body, string refusal)
    {
        var errors = Compile(Head + body + Proc);
        Assert.True(errors.Any(e => e.Contains("error " + refusal)), $"[{name}] no refusal by name:\n{string.Join("\n", errors)}");
        Assert.DoesNotContain(errors, e => e.Contains("period may be missing") || e.Contains("COBOL0001"));
    }

    /// <summary>A sign SEPARATED from its digits at the run's opening is screened as it is in every other signed literal
    /// slot (COBOLNET2155, ISO §8.3.3.3.2 2) — the run's tail holds <c>signedNumericLiteral</c>, which LiteralScreen
    /// already screened), and the run is still refused once, quoting the literal as written.</summary>
    [Fact]
    public void SeparatedSignAtTheOpening_IsScreenedAndRefused()
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ - 1.\n" + Proc);
        Assert.True(errors.Count == 2, $"expected the sign screen and the refusal, got:\n{string.Join("\n", errors)}");
        Assert.Contains(errors, e => e.Contains("error COBOLNET2155: "));
        Assert.Contains(errors, e => e.Contains("error COBOLNET1970: '- 1' is not a clause of the SOURCE-COMPUTER paragraph"));
    }

    [Theory]
    [MemberData(nameof(LiteralResidue))]
    public void LiteralInAClosedFormat_IsRefusedByName_Once(string name, string source, string code, string sentence)
    {
        var errors = Compile(source);
        Assert.True(errors.Count == 1, $"[{name}] expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains($"error {code}: ", errors[0]);
        Assert.Contains(sentence, errors[0]);
        Assert.Contains("is a closed list", errors[0]);
        // The "a data-name is expected here" guess yields to the structural refusal (kb/Work PB3373).
        Assert.DoesNotContain("data-name is expected", errors[0]);
    }

    /// <summary>⛔ A WORD REFUSED BY A TEXT-PREDICATED CLAUSE IS REFUSED BY NAME, IN ITS OWN FORMAT (kb/Work PB3391). The
    /// error production opens at a word, but where a clause of the same list is recognized by the TEXT of its word
    /// (SEGMENT-LIMIT, MEMORY SIZE, CLASSIFICATION, the SPECIAL-NAMES LOCALE clause, PICTURE's LOCALE phrase) and the
    /// run cannot continue at the reserved word that clause takes next (IS, SIZE), the predicated clause is the
    /// prediction's sole survivor, and ANTLR throws NoViableAlt at the word when its predicate refuses it: a bare
    /// COBOL0001 "cannot parse construct near 'FOO'", and a second refusal of the next token against an enclosing
    /// list's format. The word is now refused in its own format, and the run is ONE refusal. Every edition: the
    /// standard prints none of these words as a clause (ISO §12.3.6.2, §12.3.7.2, §13.16.2), and at COBOL-85, where
    /// SEGMENT-LIMIT and MEMORY SIZE were clauses, FOO is still neither.</summary>
    public static TheoryData<string, int, string, string, string> PredicateRefusedWord() => new()
    {
        // The note's repro: the run FOO ends at IS, and only `{segmentLimitAhead()}? IDENTIFIER IS? integerLiteral` survives.
        { "object-computer-segment-limit-shape", 85, "       OBJECT-COMPUTER. XYZ FOO IS 12.\n",
          "COBOLNET1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-segment-limit-shape-2023", 2023, "       OBJECT-COMPUTER. XYZ FOO IS 12.\n",
          "COBOLNET1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // The shape's prefix alone: the prediction commits at IS, before the period that would end it.
        { "object-computer-segment-limit-prefix", 85, "       OBJECT-COMPUTER. XYZ FOO IS.\n",
          "COBOLNET1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // Without the computer-name: the optional name's decision fails the same way.
        { "object-computer-segment-limit-shape-without-name", 85, "       OBJECT-COMPUTER. FOO IS 12.\n",
          "COBOLNET1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // MEMORY SIZE: SIZE is reserved, so the run FOO ends there and the predicated clause survives.
        { "object-computer-memory-size-shape", 85, "       OBJECT-COMPUTER. XYZ FOO SIZE 12 WORDS.\n",
          "COBOLNET1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // INSIDE the run: the run's own loop decides between continuing with BAR and ending before a clause opening at BAR.
        { "object-computer-segment-limit-shape-in-run", 85, "       OBJECT-COMPUTER. XYZ FOO BAR IS 12.\n",
          "COBOLNET1970", "'BAR' is not a clause of the OBJECT-COMPUTER paragraph" },
        // CHARACTER CLASSIFICATION's CHARACTER is optional and its CLASSIFICATION text-predicated: a reserved word the arm
        // already named, whose residue drew a second refusal at IS ("not a paragraph of the CONFIGURATION SECTION").
        { "object-computer-character-shape", 2023, "       OBJECT-COMPUTER. XYZ CHARACTER FOO IS X.\n",
          "COBOLNET1970", "'CHARACTER' is not a clause of the OBJECT-COMPUTER paragraph" },
        // The SPECIAL-NAMES LOCALE clause `{localeClauseAhead()}? formatWord cobolWord IS? (cobolWord | literal)`.
        { "special-names-locale-shape", 2023, "       SPECIAL-NAMES.\n           FOO BAR IS \"x\".\n",
          "COBOLNET1970", "'FOO' is not a clause of the SPECIAL-NAMES paragraph" },
        { "special-names-locale-shape-85", 85, "       SPECIAL-NAMES.\n           FOO BAR IS \"x\".\n",
          "COBOLNET1970", "'FOO' is not a clause of the SPECIAL-NAMES paragraph" },
    };

    [Theory]
    [MemberData(nameof(PredicateRefusedWord))]
    public void WordRefusedByATextPredicatedClause_IsRefusedByName_Once(string name, int edition, string paragraph, string code, string sentence)
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n" + paragraph + Proc, edition);
        Assert.True(errors.Count == 1, $"[{name}] expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains($"error {code}: ", errors[0]);
        Assert.Contains(sentence, errors[0]);
        Assert.Contains("is a closed list", errors[0]);
    }

    /// <summary>The data division's sibling (kb/Work PB3391): PICTURE's LOCALE phrase is text-predicated, so after
    /// <c>PIC X</c> a word followed by SIZE leaves the phrase the sole survivor. The word is refused in the data
    /// description entry's format, and the recovery stops at the entry's period: the NEXT entry's error is still
    /// reported, in its own entry.</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2023)]
    public void WordRefusedByThePictureLocalePhrase_IsRefusedInItsEntry_AndTheNextEntryStillChecked(int edition)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO SIZE 3.\n"
            + "       01 W-Y PIC X BLOCK.\n" + Proc, edition);
        Assert.True(errors.Count == 2, $"expected exactly two diagnostics, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error COBOLNET1941: ", errors[0]);
        Assert.Contains("'FOO' is not a clause of the data description entry for 'W-X'", errors[0]);
        Assert.Contains("'BLOCK' is not a clause of the data description entry for 'W-Y'", errors[1]);
    }

    /// <summary>The recovery after a refused word consumes its run, never the next paragraph (kb/Work PB3391): the
    /// SPECIAL-NAMES paragraph after <c>OBJECT-COMPUTER. XYZ FOO IS 12.</c> is still parsed, and its own error is
    /// still reported in its own format.</summary>
    [Fact]
    public void WordRefusedByATextPredicatedClause_LeavesTheNextParagraphChecked()
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ FOO IS 12.\n"
            + "       SPECIAL-NAMES. GLOBAL.\n" + Proc, 85);
        Assert.True(errors.Count == 2, $"expected exactly two diagnostics, got:\n{string.Join("\n", errors)}");
        Assert.Contains("'FOO' is not a clause of the OBJECT-COMPUTER paragraph", errors[0]);
        Assert.Contains("'GLOBAL' is not a clause of the SPECIAL-NAMES paragraph", errors[1]);
    }

    /// <summary>The word the predicate ACCEPTS keeps its clause (kb/Work PB3391's controls): <c>SEGMENT-LIMIT IS</c> with
    /// its integer missing is a malformed clause of the 85 format, never "'SEGMENT-LIMIT' is not a clause".</summary>
    [Fact]
    public void MalformedTextPredicatedClause_IsNotCalledForeign()
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ SEGMENT-LIMIT IS.\n" + Proc, 85);
        Assert.NotEmpty(errors);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1970"));
    }

    /// <summary>At COBOL-85 the misreading also drew edition diagnostics about a word it had misplaced (kb/Work
    /// PB3373): XYZ read as a configuration paragraph was an area-A header out of area A (COBOLNET3734), and XYZ read as
    /// an OBJECT-COMPUTER clause was a clause without computer-name-1 (COBOLNET0900). A residue run is no clause either,
    /// so <c>OBJECT-COMPUTER. "LIT".</c> keeps its one refusal too.</summary>
    [Theory]
    [InlineData("SOURCE-COMPUTER. XYZ \"LIT\".", "SOURCE-COMPUTER")]
    [InlineData("OBJECT-COMPUTER. XYZ \"LIT\".", "OBJECT-COMPUTER")]
    [InlineData("OBJECT-COMPUTER. \"LIT\".", "OBJECT-COMPUTER")]
    public void LiteralAfterTheComputerName_AtCobol85_IsTheOneRefusal(string paragraph, string subject)
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       " + paragraph + "\n" + Proc, 85);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains($"error COBOLNET1970: '\"LIT\"' is not a clause of the {subject} paragraph", errors[0]);
    }

    /// <summary>An unsigned integer the production never opens with keeps the missing-period diagnosis where it may be
    /// the next data description entry's level-number (ISO §8.3.2.2.13): <c>01 W-X PIC X</c> then <c>05 W-Y PIC X.</c>
    /// is a forgotten period, never "'05' is not a clause".</summary>
    [Fact]
    public void LevelNumberAfterAMissingPeriod_IsNotCalledAClause()
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X\n       05 W-Y PIC X.\n" + Proc);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error COBOL0307: ", errors[0]);
        Assert.Contains("'05'", errors[0]);
    }

    [Theory]
    [MemberData(nameof(ReservedWordResidue))]
    public void ReservedWordInAClosedFormat_IsRefusedByName_Once(string name, string source, string code, string sentence)
    {
        var errors = Compile(source);
        Assert.True(errors.Count == 1, $"[{name}] expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains($"error {code}: ", errors[0]);
        Assert.Contains(sentence, errors[0]);
        Assert.Contains("is a closed list", errors[0]);
    }

    /// <summary>A word that could begin what follows the entry's separator period is a MISSING PERIOD, not a stray
    /// clause: <c>01 X PIC X</c> then <c>PROCEDURE DIVISION.</c> keeps the missing-period diagnosis.</summary>
    [Fact]
    public void MissingPeriodBeforeTheNextConstruct_IsNotCalledAClause()
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X\n" + Proc);
        Assert.Contains(errors, e => e.Contains("error COBOL0307: ") && e.Contains("'PROCEDURE'"));
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1941"));
    }

    /// <summary>A computer-name with its second period missing before the next paragraph header is a MISSING PERIOD,
    /// refused by name (COBOLNET3408, kb/Work PB3370) — never "OBJECT-COMPUTER is not a clause".</summary>
    [Fact]
    public void ComputerNameWithoutItsPeriod_IsTheMissingPeriod()
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ\n"
            + "       OBJECT-COMPUTER. XYZ.\n" + Proc);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error COBOLNET3408: SOURCE-COMPUTER paragraph: it ends after 'XYZ' without a separator period", errors[0]);
    }

    /// <summary>A stray token right before a superset-parsed period is deleted as it would be before a required one
    /// (kb/Work PB3370): the paragraph keeps its period, and the error in the NEXT paragraph is still reported rather
    /// than swallowed by a recovery several divisions up.</summary>
    [Theory]
    [InlineData("SOURCE-COMPUTER. XYZ ) .")]
    [InlineData("OBJECT-COMPUTER. XYZ ) .")]
    [InlineData("SOURCE-COMPUTER. ) .")]
    public void StrayTokenBeforeAnOptionalPeriod_IsDeleted_AndTheNextErrorStillReported(string paragraph)
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       " + paragraph + "\n"
            + "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\" ) .\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc);
        Assert.True(errors.Count == 2, $"expected two diagnostics, got:\n{string.Join("\n", errors)}");
        Assert.Contains("(5,", errors[0]);
        Assert.Contains("error COBOL0307: unexpected ')'", errors[0]);
        Assert.Contains("(8,", errors[1]);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET3408"));
    }

    /// <summary>A superset-parsed paragraph whose period is missing, followed by a paragraph header the position cannot
    /// take (FILE-CONTROL. with INPUT-OUTPUT SECTION forgotten), keeps that header: the stray-token deletion before an
    /// optional period never deletes a header word (kb/Work PB3370 review R1-1). Deleting it poured the SELECT entries
    /// into the closed format above as false "not a clause" refusals. One case per <c>ParagraphPeriods</c> row, each
    /// with an entry written, and the header is the ONE diagnostic, at the header.</summary>
    public static TheoryData<string, string, string> HeaderAfterMissingPeriod() => new()
    {
        { "SPECIAL-NAMES", "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES. DECIMAL-POINT IS COMMA\n", "(6," },
        { "REPOSITORY", "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       REPOSITORY.\n           FUNCTION ALL INTRINSIC\n", "(7," },
        { "SOURCE-COMPUTER", "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ\n", "(6," },
        { "OBJECT-COMPUTER", "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ\n", "(6," },
        { "I-O-CONTROL", "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       I-O-CONTROL.\n           SAME RECORD AREA FOR SF TF\n", "(7," },
    };

    [Theory]
    [MemberData(nameof(HeaderAfterMissingPeriod))]
    public void HeaderAfterAMissingPeriod_IsNotDeleted_NorItsEntriesCalledClauses(string paragraph, string section, string at)
    {
        var errors = Compile(Head + section + "       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\".\n"
            + "           SELECT TF ASSIGN TO \"g.dat\".\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n"
            + "       01 R PIC X.\n       FD TF.\n       01 S PIC X.\n" + Proc);
        Assert.True(errors.Count == 1, $"{paragraph}: expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains(at, errors[0]);
        Assert.Contains("'FILE-CONTROL'", errors[0]);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1970") || e.Contains("COBOLNET1971"));
    }

    /// <summary>The theory above holds a case for EVERY superset-parsed paragraph, so a paragraph that joins the table
    /// joins this control too.</summary>
    [Fact]
    public void HeaderAfterMissingPeriod_CoversEveryParagraphPeriodsRow()
    {
        var covered = HeaderAfterMissingPeriod().Select(row => (string)row[0]).ToHashSet();
        var rows = CobolNet.Frontend.Cst.ParagraphPeriods.ByContext.Values.Select(p => p.Subject).ToHashSet();
        Assert.True(rows.SetEquals(covered), $"rows {string.Join(", ", rows.Order())} vs cases {string.Join(", ", covered.Order())}");
    }

    /// <summary>A paragraph header written out of order (I-O-CONTROL before FILE-CONTROL) is a structure error; its
    /// opening word is never named "not a clause of the I-O-CONTROL paragraph".</summary>
    [Fact]
    public void ParagraphHeaderOutOfOrder_IsNotCalledAClause()
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       I-O-CONTROL.\n"
            + "       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\".\n       DATA DIVISION.\n       FILE SECTION.\n"
            + "       FD SF.\n       01 R PIC X.\n" + Proc);
        Assert.NotEmpty(errors);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1970"));
    }

    /// <summary>A clause the format DOES print, written malformed (<c>SD SF RECORD.</c>), fails inside that clause; the
    /// arm never calls RECORD "not a clause of the sort-merge file description entry".</summary>
    [Fact]
    public void MalformedClauseOfTheFormat_IsNotCalledForeign()
    {
        var errors = Compile(Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF RECORD.\n       01 R PIC X.\n" + Proc);
        Assert.NotEmpty(errors);
        Assert.DoesNotContain(errors, e => e.Contains("COBOLNET1970"));
    }

    private static IReadOnlyList<string> Compile(string source, int edition = 2023)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB2533_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb2533u.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb2533u.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            Assert.False(r.Success, "the source is non-conforming and must be rejected");
            return r.Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
