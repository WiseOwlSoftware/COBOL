// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Frontend.Preprocessor;
using Xunit;
using WiseOwl.Cobol.Tests.Shared;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// ⛔ A RESERVED WORD IN A CLOSED GENERAL FORMAT IS REFUSED BY NAME (kb/Work PB2533). Every closed format ends its
/// alternative list in the error production (<c>unrecognizedClause</c>, or <c>unrecognizedDataClause</c> in the data
/// division), which <c>ClosedFormatPass</c> refuses naming the format (COBOL1941 / 1970 / 1971) — but the production
/// never begins at a reserved word, so one the format does not print (<c>SD SF GLOBAL.</c>) never entered it and drew
/// a bare parse error. <c>CobolErrorStrategy</c> now finds the production's position in the ATN and reports the pass's
/// own refusal, from the same <c>ClosedFormats</c> row.
/// A LITERAL is held here too (kb/Work PB3373, PB3390, <see cref="LiteralResidue"/>): the production opens at any
/// literal, but the data division's form holds none that begins with an unsigned integer (the next entry's
/// level-number may stand there), and the parse layer refuses the comma decimal it cannot hold.
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
    private const string Proc = "       PROCEDURE DIVISION.\n       MAIN-P.\n           STOP RUN.\n";
    private const string CommaMode = "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n"
        + "           DECIMAL-POINT IS COMMA.\n";

    public static TheoryData<string, string, string, string> ReservedWordResidue() => new()
    {
        // ⛔ A FOREIGN RUN A RESERVED WORD ENDS IS REFUSED BY ITS OPENING WORD (kb/Work PB4301): the misspelled clause
        // keyword is the fault, not the clause word after it that the run happened to meet.
        { "select-misspelled-assign", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASIGN TO \"f.dat\".\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'ASIGN' is not a clause of the file control entry for 'SF'" },
        { "data-entry-misspelled-sync", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC 9 SINC LEFT.\n" + Proc,
          "COBOL1941", "'SINC' is not a clause of the data description entry for 'W-X'" },
        // The data division's run holds no integer: it ends at the 3, which a level-number may be, and the whole entry is
        // still the one refusal (no level-3 entry `3 TIMES` is made of its tail).
        { "data-entry-misspelled-occurs-before-integer", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC 9(3) OCURS 3 TIMES.\n" + Proc,
          "COBOL1941", "'OCURS' is not a clause of the data description entry for 'W-X'" },
        // §13.16.2 Format 1 — after a complete entry, so the clause list has already ended when BLOCK fails.
        { "data-entry", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X BLOCK CONTAINS 10.\n" + Proc,
          "COBOL1941", "'BLOCK' is not a clause of the data description entry for 'W-X'" },
        // §13.16.2 again, on a level-88 entry: its clauses are the same alternative list.
        { "condition-entry", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X.\n          88 W-C VALUE \"A\" BLOCK.\n" + Proc,
          "COBOL1941", "'BLOCK' is not a clause of the data description entry for 'W-C'" },
        // §13.4.5.2 — the FD.
        { "fd", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF DUPLICATES.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'DUPLICATES' is not a clause of the file description entry for 'SF'" },
        // §13.4.6.2 — the SD, with no clause written (the note's repro) ...
        { "sd-global", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF GLOBAL.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'GLOBAL' is not a clause of the sort-merge file description entry for 'SF'" },
        // ... and after a complete RECORD clause, where the list may end and the base Sync let the word pass.
        { "sd-after-record", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF RECORD 80 EXTERNAL.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'EXTERNAL' is not a clause of the sort-merge file description entry for 'SF'" },
        // A multi-word residue is ONE refusal: the recovery consumes it to the period.
        { "sd-block-contains", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF BLOCK CONTAINS 10 RECORDS.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'BLOCK' is not a clause of the sort-merge file description entry for 'SF'" },
        // §12.4.5.1 — the file control entry.
        { "select", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\" GLOBAL.\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'GLOBAL' is not a clause of the file control entry for 'SF'" },
        // §12.4.6.2 — the I-O-CONTROL paragraph.
        { "io-control", Head + Select + "       I-O-CONTROL.\n           GLOBAL.\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'GLOBAL' is not a clause of the I-O-CONTROL paragraph" },
        // §12.3.7.2 — the SPECIAL-NAMES paragraph: the INNERMOST open list names it, not the configuration section's.
        { "special-names", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           GLOBAL.\n" + Proc,
          "COBOL1970", "'GLOBAL' is not a clause of the SPECIAL-NAMES paragraph" },
        // §12.3.6.2 — the OBJECT-COMPUTER paragraph, whose clauses may follow its period with no computer-name.
        // (SOURCE-COMPUTER prints nothing but the name there, so `SOURCE-COMPUTER. GLOBAL.` reads as a configuration
        // section paragraph — the configuration-section case below.)
        { "object-computer", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. GLOBAL.\n" + Proc,
          "COBOL1970", "'GLOBAL' is not a clause of the OBJECT-COMPUTER paragraph" },
        // §12.3.5.2 / §12.3.6.2 AFTER the computer-name (kb/Work PB3370): XYZ could be the name or an unrecognized
        // configuration paragraph, both readings fail at GLOBAL, and the error-time prediction used to take the
        // configuration section's (a bare COBOL0307). The paragraphs' superset-parsed second period makes the name win.
        { "source-computer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ GLOBAL.\n" + Proc,
          "COBOL1970", "'GLOBAL' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ GLOBAL.\n" + Proc,
          "COBOL1970", "'GLOBAL' is not a clause of the OBJECT-COMPUTER paragraph" },
        // ... and after an unrecognized word run, where no list loop of the format's own is left to Sync: the residue
        // continues inside the production (CobolErrorStrategy reads the parser's own call of it). The word refused is the
        // run's OPENING, WIBBLE, as ClosedFormatPass quotes it, never the GLOBAL that ended the run (kb/Work PB4301).
        { "source-computer-after-residue", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ WIBBLE GLOBAL.\n" + Proc,
          "COBOL1970", "'WIBBLE' is not a clause of the SOURCE-COMPUTER paragraph" },
        // §12.3.2 — the configuration section's PARAGRAPH list.
        { "configuration-section", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       GLOBAL.\n" + Proc,
          "COBOL1971", "'GLOBAL' is not a paragraph of the CONFIGURATION SECTION" },
        { "configuration-section-after-residue", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       WIBBLE GLOBAL.\n" + Proc,
          "COBOL1971", "'WIBBLE' is not a paragraph of the CONFIGURATION SECTION" },
        // §11.2.1 — the identification division's PARAGRAPH list.
        { "identification", Head + "       GLOBAL.\n" + Proc,
          "COBOL1971", "'GLOBAL' is not a paragraph of the IDENTIFICATION DIVISION" },
        { "identification-after-residue", Head + "       WIBBLE GLOBAL.\n" + Proc,
          "COBOL1971", "'WIBBLE' is not a paragraph of the IDENTIFICATION DIVISION" },
        // A FIGURATIVE CONSTANT after the computer-name is a reserved word too (kb/Work PB3373): the word run's tail used
        // to be the whole `literal` rule, which read `XYZ ZERO` as one run and XYZ as the clause.
        { "object-computer-figurative-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ ZERO.\n" + Proc,
          "COBOL1970", "'ZERO' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "source-computer-figurative-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ ZERO.\n" + Proc,
          "COBOL1970", "'ZERO' is not a clause of the SOURCE-COMPUTER paragraph" },
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
          "COBOL1970", "'\"LIT\"' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "source-computer-decimal-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ 1.5.\n" + Proc,
          "COBOL1970", "'1.5' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ \"LIT\".\n" + Proc,
          "COBOL1970", "'\"LIT\"' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-without-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. N\"LIT\".\n" + Proc,
          "COBOL1970", "'N\"LIT\"' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-integer", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. 12.\n" + Proc,
          "COBOL1970", "'12' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "special-names", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA B\"1\".\n" + Proc,
          "COBOL1970", "'B\"1\"' is not a clause of the SPECIAL-NAMES paragraph" },
        { "configuration-section", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       \"LIT\".\n" + Proc,
          "COBOL1971", "'\"LIT\"' is not a paragraph of the CONFIGURATION SECTION" },
        { "identification", Head + "       X\"41\".\n" + Proc,
          "COBOL1971", "'X\"41\"' is not a paragraph of the IDENTIFICATION DIVISION" },
        { "select", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\"\n"
            + "               ORGANIZATION IS SEQUENTIAL \"LIT\".\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'\"LIT\"' is not a clause of the file control entry for 'SF'" },
        { "io-control", Head + Select + "       I-O-CONTROL.\n           \"LIT\".\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'\"LIT\"' is not a clause of the I-O-CONTROL paragraph" },
        { "fd", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF \"LIT\".\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'\"LIT\"' is not a clause of the file description entry for 'SF'" },
        { "sd", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF \"LIT\".\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'\"LIT\"' is not a clause of the sort-merge file description entry for 'SF'" },
        { "data-entry", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X \"A\".\n" + Proc,
          "COBOL1941", "'\"A\"' is not a clause of the data description entry for 'W-X'" },
        // A LITERAL OF MORE THAN ONE TOKEN is a literal too (kb/Work PB3373, review finding R1-1): a sign-led numeric
        // literal (ISO §8.3.3.3.2 2), MINUS INTEGERLIT outside the list regions), a concatenation expression (§8.8.3.3
        // 3)) and a DECIMAL-POINT IS COMMA literal. The run opens at a sign-led one and continues through all three,
        // where it used to end at the sign, the `&` or the comma with a bare parse error and a false missing period.
        { "source-computer-signed-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ +1.5.\n" + Proc,
          "COBOL1970", "'+1.5' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "source-computer-concatenation-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ \"A\" & \"B\".\n" + Proc,
          "COBOL1970", "'\"A\"' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-signed-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ -1.\n" + Proc,
          "COBOL1970", "'-1' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-signed-without-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. -1.\n" + Proc,
          "COBOL1970", "'-1' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-signed-in-run", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ FOO -1 \"A\" & ZERO.\n" + Proc,
          "COBOL1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "data-entry-signed-opening", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X -1.\n" + Proc,
          "COBOL1941", "'-1' is not a clause of the data description entry for 'W-X'" },
        { "data-entry-signed-in-run", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO -1.\n" + Proc,
          "COBOL1941", "'FOO' is not a clause of the data description entry for 'W-X'" },
        { "data-entry-concatenation-in-run", Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO \"A\" & \"B\".\n" + Proc,
          "COBOL1941", "'FOO' is not a clause of the data description entry for 'W-X'" },
        // The data division's run holds no item that begins with an unsigned integer (kb/Work PB3390), so the run FOO
        // ends at `1,5`; the comma decimal is one character-string and never a level-number, so the parse layer
        // refuses the run rather than guess a missing period, by the word that OPENED it, FOO (kb/Work PB4301:
        // ClosedFormatPass quotes the same word), not the literal that ended it.
        { "data-entry-comma-decimal-in-run", Head + CommaMode + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO 1,5.\n" + Proc,
          "COBOL1941", "'FOO' is not a clause of the data description entry for 'W-X'" },
        { "data-entry-comma-decimal-opening", Head + CommaMode + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X 1,5.\n" + Proc,
          "COBOL1941", "'1,5' is not a clause of the data description entry for 'W-X'" },
        { "data-entry-leading-comma-opening", Head + CommaMode + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X ,5.\n" + Proc,
          "COBOL1941", "',5' is not a clause of the data description entry for 'W-X'" },
        { "fd-comma-decimal-opening", Head + CommaMode + "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\".\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF 1,5.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'1,5' is not a clause of the file description entry for 'SF'" },
        { "sd-leading-comma-in-run", Head + CommaMode + "       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\".\n"
            + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF RECORD 80 FOO ,5.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'FOO' is not a clause of the sort-merge file description entry for 'SF'" },
        // THE UNSIGNED INTEGER AND THE COMMA DECIMALS outside the data division (kb/Work PB3390): no level-number follows
        // these lists, so the run opens and continues at them, and the computer-name keeps its reading.
        { "source-computer-integer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ 12.\n" + Proc,
          "COBOL1970", "'12' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-integer-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ 12.\n" + Proc,
          "COBOL1970", "'12' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "source-computer-leading-comma-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ ,5.\n"
            + "       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA.\n" + Proc,
          "COBOL1970", "',5' is not a clause of the SOURCE-COMPUTER paragraph" },
        { "object-computer-leading-comma-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ ,5.\n"
            + "       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA.\n" + Proc,
          "COBOL1970", "',5' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-comma-decimal-after-name", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ 1,5.\n"
            + "       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA.\n" + Proc,
          "COBOL1970", "'1,5' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-integer-in-run", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       OBJECT-COMPUTER. XYZ FOO 12 BAR.\n" + Proc,
          "COBOL1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "special-names-integer", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           DECIMAL-POINT IS COMMA 12.\n" + Proc,
          "COBOL1970", "'12' is not a clause of the SPECIAL-NAMES paragraph" },
        { "configuration-section-integer", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       12.\n" + Proc,
          "COBOL1971", "'12' is not a paragraph of the CONFIGURATION SECTION" },
        { "identification-integer", Head + "       12.\n" + Proc,
          "COBOL1971", "'12' is not a paragraph of the IDENTIFICATION DIVISION" },
        { "select-integer", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\"\n"
            + "               ORGANIZATION IS SEQUENTIAL 12.\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'12' is not a clause of the file control entry for 'SF'" },
        { "io-control-integer", Head + Select + "       I-O-CONTROL.\n           12.\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'12' is not a clause of the I-O-CONTROL paragraph" },
        { "fd-signed-in-run", Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF FOO +2.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'FOO' is not a clause of the file description entry for 'SF'" },
        { "select-signed-in-run", Head + "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\"\n"
            + "               ORGANIZATION IS SEQUENTIAL FOO +2.\n       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n" + Proc,
          "COBOL1970", "'FOO' is not a clause of the file control entry for 'SF'" },
    };

    /// <summary>A sign-led literal after a clause that takes another word (kb/Work PB3373, review finding R1-1):
    /// <c>FOO BAR</c> is an implementor-name clause's shape and <c>ASSIGN TO "f.dat" FOO</c> an ASSIGN operand list, so
    /// the clause keeps them (loop continuation, the lower alternative, as for a one-token literal) and the run opens at
    /// the sign-led literal, which is refused by name — never the bare parse error with a false missing period it drew
    /// while the run could not hold a sign.</summary>
    [Theory]
    [InlineData("special-names", "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SPECIAL-NAMES.\n           FOO BAR -1.\n",
        "COBOL1970: '-1' is not a clause of the SPECIAL-NAMES paragraph")]
    [InlineData("select", "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\" FOO +2.\n"
        + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF.\n       01 R PIC X.\n",
        "COBOL1970: '+2' is not a clause of the file control entry for 'SF'")]
    public void SignedLiteralAfterAWordTakingClause_IsRefusedByName(string name, string body, string refusal)
    {
        var errors = Compile(Head + body + Proc);
        Assert.True(errors.Any(e => e.Contains("error " + refusal)), $"[{name}] no refusal by name:\n{string.Join("\n", errors)}");
        Assert.DoesNotContain(errors, e => e.Contains("period may be missing") || e.Contains("COBOL0001"));
    }

    /// <summary>A sign SEPARATED from its digits at the run's opening is screened as it is in every other signed literal
    /// slot (COBOL2155, ISO §8.3.3.3.2 2) — the run's tail holds <c>signedNumericLiteral</c>, which LiteralScreen
    /// already screened), and the run is still refused once, quoting the literal as written.</summary>
    [Fact]
    public void SeparatedSignAtTheOpening_IsScreenedAndRefused()
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ - 1.\n" + Proc);
        Assert.True(errors.Count == 2, $"expected the sign screen and the refusal, got:\n{string.Join("\n", errors)}");
        Assert.Contains(errors, e => e.Contains("error COBOL2155: "));
        Assert.Contains(errors, e => e.Contains("error COBOL1970: '- 1' is not a clause of the SOURCE-COMPUTER paragraph"));
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
          "COBOL1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        { "object-computer-segment-limit-shape-2023", 2023, "       OBJECT-COMPUTER. XYZ FOO IS 12.\n",
          "COBOL1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // The shape's prefix alone: the prediction commits at IS, before the period that would end it.
        { "object-computer-segment-limit-prefix", 85, "       OBJECT-COMPUTER. XYZ FOO IS.\n",
          "COBOL1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // Without the computer-name: the optional name's decision fails the same way.
        { "object-computer-segment-limit-shape-without-name", 85, "       OBJECT-COMPUTER. FOO IS 12.\n",
          "COBOL1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // MEMORY SIZE: SIZE is reserved, so the run FOO ends there and the predicated clause survives.
        { "object-computer-memory-size-shape", 85, "       OBJECT-COMPUTER. XYZ FOO SIZE 12 WORDS.\n",
          "COBOL1970", "'FOO' is not a clause of the OBJECT-COMPUTER paragraph" },
        // INSIDE the run: the run's own loop decides between continuing with BAR and ending before a clause opening at BAR.
        { "object-computer-segment-limit-shape-in-run", 85, "       OBJECT-COMPUTER. XYZ FOO BAR IS 12.\n",
          "COBOL1970", "'BAR' is not a clause of the OBJECT-COMPUTER paragraph" },
        // CHARACTER CLASSIFICATION's CHARACTER is optional and its CLASSIFICATION text-predicated: a reserved word the arm
        // already named, whose residue drew a second refusal at IS ("not a paragraph of the CONFIGURATION SECTION").
        { "object-computer-character-shape", 2023, "       OBJECT-COMPUTER. XYZ CHARACTER FOO IS X.\n",
          "COBOL1970", "'CHARACTER' is not a clause of the OBJECT-COMPUTER paragraph" },
        // The SPECIAL-NAMES LOCALE clause `{localeClauseAhead()}? formatWord cobolWord IS? (cobolWord | literal)`.
        { "special-names-locale-shape", 2023, "       SPECIAL-NAMES.\n           FOO BAR IS \"x\".\n",
          "COBOL1970", "'FOO' is not a clause of the SPECIAL-NAMES paragraph" },
        { "special-names-locale-shape-85", 85, "       SPECIAL-NAMES.\n           FOO BAR IS \"x\".\n",
          "COBOL1970", "'FOO' is not a clause of the SPECIAL-NAMES paragraph" },
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
        Assert.Contains("error COBOL1941: ", errors[0]);
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
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1970"));
    }

    /// <summary>At COBOL-85 the misreading also drew edition diagnostics about a word it had misplaced (kb/Work
    /// PB3373): XYZ read as a configuration paragraph was an area-A header out of area A (COBOL3734), and XYZ read as
    /// an OBJECT-COMPUTER clause was a clause without computer-name-1 (COBOL0900). A residue run is no clause either,
    /// so <c>OBJECT-COMPUTER. "LIT".</c> keeps its one refusal too.</summary>
    [Theory]
    [InlineData("SOURCE-COMPUTER. XYZ \"LIT\".", "SOURCE-COMPUTER")]
    [InlineData("OBJECT-COMPUTER. XYZ \"LIT\".", "OBJECT-COMPUTER")]
    [InlineData("OBJECT-COMPUTER. \"LIT\".", "OBJECT-COMPUTER")]
    public void LiteralAfterTheComputerName_AtCobol85_IsTheOneRefusal(string paragraph, string subject)
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       " + paragraph + "\n" + Proc, 85);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains($"error COBOL1970: '\"LIT\"' is not a clause of the {subject} paragraph", errors[0]);
    }

    /// <summary>An unsigned integer the data division's run never holds keeps the missing-period diagnosis where it may
    /// be the next entry's level-number (ISO §8.3.2.2.13): <c>01 W-X PIC X</c> then <c>05 W-Y PIC X.</c> is a forgotten
    /// period, never "'05' is not a clause". One case per list a level-number follows.</summary>
    [Fact]
    public void LevelNumberAfterAMissingPeriod_IsNotCalledAClause()
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X\n       05 W-Y PIC X.\n" + Proc);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error COBOL0307: ", errors[0]);
        Assert.Contains("'05'", errors[0]);
        Assert.Contains("period may be missing", errors[0]);
    }

    /// <summary>After a foreign word (kb/Work PB3390) the level-number is never the one run <c>FOO 05 W-Y</c>, which read
    /// W-Y's PICTURE as W-X's second (COBOL2423); the missing period is reported, and so is the run (kb/Work PB4301):
    /// FOO is no clause whichever way the period goes, and <c>ClosedFormatPass</c>, which would name it, never runs once
    /// the unit has a parse error. One case per list a level-number follows; the run's refusal comes first, at its word.</summary>
    [Theory]
    [InlineData("data-entry-after-run", "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC X FOO\n       05 W-Y PIC X.\n",
        "COBOL1941", "'FOO' is not a clause of the data description entry for 'W-X'", "'05'")]
    // `VALEU 5.`: the 5 may be the next entry's level-number, so the missing period is reported too; VALEU is the fault either way.
    [InlineData("data-entry-misspelled-value", "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       01 W-X PIC 9 VALEU 5.\n",
        "COBOL1941", "'VALEU' is not a clause of the data description entry for 'W-X'", "'5'")]
    [InlineData("fd-after-run", "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\".\n"
        + "       DATA DIVISION.\n       FILE SECTION.\n       FD SF FOO\n       01 R PIC X.\n",
        "COBOL1970", "'FOO' is not a clause of the file description entry for 'SF'", "'01'")]
    [InlineData("sd-after-run", "       ENVIRONMENT DIVISION.\n       INPUT-OUTPUT SECTION.\n       FILE-CONTROL.\n           SELECT SF ASSIGN TO \"f.dat\".\n"
        + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF FOO\n       01 R PIC X.\n",
        "COBOL1970", "'FOO' is not a clause of the sort-merge file description entry for 'SF'", "'01'")]
    public void LevelNumberAfterAForeignRun_IsTheRunsRefusalAndTheMissingPeriod(string name, string body, string code, string sentence, string at)
    {
        var errors = Compile(Head + body + Proc);
        Assert.True(errors.Count == 2, $"[{name}] expected the run's refusal and the missing period, got:\n{string.Join("\n", errors)}");
        Assert.Contains($"error {code}: ", errors[0]);
        Assert.Contains(sentence, errors[0]);
        Assert.Contains("error COBOL0307: ", errors[1]);
        Assert.Contains(at, errors[1]);
        Assert.Contains("period may be missing", errors[1]);
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
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1941"));
    }

    /// <summary>A computer-name with its second period missing before the next paragraph header is a MISSING PERIOD,
    /// refused by name (COBOL3408, kb/Work PB3370) — never "OBJECT-COMPUTER is not a clause".</summary>
    [Fact]
    public void ComputerNameWithoutItsPeriod_IsTheMissingPeriod()
    {
        var errors = Compile(Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       SOURCE-COMPUTER. XYZ\n"
            + "       OBJECT-COMPUTER. XYZ.\n" + Proc);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error COBOL3408: SOURCE-COMPUTER paragraph: it ends after 'XYZ' without a separator period", errors[0]);
    }

    /// <summary>A stray token right before a superset-parsed period is deleted as it would be before a required one
    /// (kb/Work PB3370): the paragraph keeps its period, and the error in the NEXT paragraph is still reported rather
    /// than swallowed by a recovery several divisions up. The stray ')' draws the bare "unexpected ')'": a period is
    /// written right after it, and none written before it would let it follow, so the guess "A period may be missing"
    /// (COBOL0307) it drew while a period merely being expected sufficed is gone (kb/Work PB3832, review R2-1).</summary>
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
        Assert.Contains("error COBOL0001: unexpected ')'", errors[0]);
        Assert.Contains("(8,", errors[1]);
        Assert.DoesNotContain(errors, e => e.Contains("COBOL3408"));
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
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1970") || e.Contains("COBOL1971"));
    }

    /// <summary>The theory above holds a case for EVERY superset-parsed paragraph, so a paragraph that joins the table
    /// joins this control too.</summary>
    [Fact]
    public void HeaderAfterMissingPeriod_CoversEveryParagraphPeriodsRow()
    {
        var covered = HeaderAfterMissingPeriod().Select(row => (string)row[0]).ToHashSet();
        var rows = WiseOwl.Cobol.Frontend.Cst.ParagraphPeriods.ByContext.Values.Select(p => p.Subject).ToHashSet();
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
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1970"));
    }

    /// <summary>A clause the format DOES print, written malformed (<c>SD SF RECORD.</c>), fails inside that clause; the
    /// arm never calls RECORD "not a clause of the sort-merge file description entry".</summary>
    [Fact]
    public void MalformedClauseOfTheFormat_IsNotCalledForeign()
    {
        var errors = Compile(Head + Select + "       DATA DIVISION.\n       FILE SECTION.\n       SD SF RECORD.\n       01 R PIC X.\n" + Proc);
        Assert.NotEmpty(errors);
        Assert.DoesNotContain(errors, e => e.Contains("COBOL1970"));
    }

    /// <summary>⛔ A RESERVED WORD WHERE THE ENTRY'S NAME IS DUE IS THE NAME THE SOURCE WROTE (kb/Work PB4604). ISO §13.16.3 SR4
    /// puts the entry-name directly after the level-number, so the first word of <c>01 MOVE PIC X.</c> is read as the
    /// name and as the first clause, and a word that is no clause is §8.3.2.1 1)'s violation. It used to draw COBOL1941
    /// "'MOVE' is not a clause of the data description entry". Every level-number of the one entry rule, a verb, a
    /// word no statement begins with, a figurative constant and a word reserved for an extension; the ONLY diagnostic,
    /// at every edition.</summary>
    [Theory]
    [InlineData("01 MOVE PIC X.", "COBOL0901: 'MOVE' is a reserved word in COBOL-")]
    [InlineData("01 MOVE.", "COBOL0901: 'MOVE' is a reserved word in COBOL-")]
    [InlineData("77 MOVE PIC X.", "COBOL0901: 'MOVE' is a reserved word in COBOL-")]
    [InlineData("01 TO PIC X.", "COBOL0901: 'TO' is a reserved word in COBOL-")]
    [InlineData("01 END-IF PIC X.", "COBOL0901: 'END-IF' is a reserved word in COBOL-")]
    [InlineData("01 EXIT PIC X.", "COBOL0901: 'EXIT' is a reserved word in COBOL-")]
    [InlineData("01 ZERO PIC X VALUE \"A\".", "COBOL0901: 'ZERO' is a reserved word in COBOL-")]
    [InlineData("01 W-G.\n          05 IF PIC X.", "COBOL0901: 'IF' is a reserved word in COBOL-")]
    [InlineData("01 W-A PIC X.\n          66 MOVE RENAMES W-A.", "COBOL0901: 'MOVE' is a reserved word in COBOL-")]
    [InlineData("01 W-A PIC X.\n          88 TO VALUE \"A\".", "COBOL0901: 'TO' is a reserved word in COBOL-")]
    [InlineData("01 ENTRY PIC X.", "COBOL0313: unexpected 'ENTRY'. ENTRY is a word this implementation reserves for a nonstandard extension")]
    [InlineData("01 JSON PIC X.", "COBOL0313: unexpected 'JSON'. JSON is a word this implementation reserves for a nonstandard extension")]
    public void ReservedWordAtTheEntryNameSlot_NamesTheReservation(string entry, string diagnostic)
    {
        foreach (int edition in new[] { 85, 2002, 2014, 2023 })
        {
            var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       " + entry + "\n" + Proc, edition);
            Assert.True(errors.Count == 1, $"[{edition}] expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
            Assert.Contains("error " + diagnostic, errors[0]);
            Assert.DoesNotContain("COBOL1941", errors[0]);
            Assert.DoesNotContain("is not a clause", errors[0]);
        }
    }

    /// <summary>The word AFTER a name or after a clause is a clause position only, so the closed-format refusal stands
    /// there (kb/Work PB4604): the name slot is closed by what is written before the word.</summary>
    [Theory]
    [InlineData("01 W-A PIC X MOVE.", "'MOVE' is not a clause of the data description entry for 'W-A'")]
    [InlineData("01 W-A MOVE.", "'MOVE' is not a clause of the data description entry for 'W-A'")]
    [InlineData("01 W-A TO.", "'TO' is not a clause of the data description entry for 'W-A'")]
    [InlineData("01 W-A PIC X.\n          05 W-B MOVE.", "'MOVE' is not a clause of the data description entry for 'W-B'")]
    [InlineData("01 FILLER MOVE.", "'MOVE' is not a clause of the data description entry for 'FILLER'")]
    [InlineData("01 PIC X MOVE.", "'MOVE' is not a clause of the data description entry — ")]
    [InlineData("01 USAGE DISPLAY MOVE.", "'MOVE' is not a clause of the data description entry — ")]
    [InlineData("01 W-A PIC X TO.", "'TO' is not a clause of the data description entry for 'W-A'")]
    public void ReservedWordAfterANameOrAClause_IsStillNotAClause(string entry, string sentence)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       " + entry + "\n" + Proc);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error COBOL1941: ", errors[0]);
        Assert.Contains(sentence, errors[0]);
    }

    /// <summary>⛔ A WORD THAT BEGINS WHAT FOLLOWS THE ENTRY IS A MISSING PERIOD, NOT A RESERVED WORD WRITTEN AS ITS NAME (kb/Work
    /// PB4646). ISO §13.16.2 ends the entry with a separator period; <c>01</c> with its period forgotten and the next
    /// section's header on the next line draws COBOL0307 at the header word, as it did before the entry-name slot named
    /// reserved words (PB4604). The word AND the one after it must begin something the grammar lets follow the entry:
    /// <c>LINKAGE PIC X.</c> is not a header, so the reserved word is a name written and keeps COBOL0901.</summary>
    [Theory]
    [InlineData("01\n       LINKAGE SECTION.", "COBOL0307: unexpected 'LINKAGE'. A period may be missing")]
    [InlineData("01 W-A PIC X.\n       01\n       LINKAGE SECTION.", "COBOL0307: unexpected 'LINKAGE'. A period may be missing")]
    [InlineData("01\n       REPORT SECTION.", "COBOL0307: unexpected 'REPORT'. A period may be missing")]
    [InlineData("01 LINKAGE PIC X.", "COBOL0901: 'LINKAGE' is a reserved word in COBOL-")]
    [InlineData("01 LINKAGE.", "COBOL0901: 'LINKAGE' is a reserved word in COBOL-")]
    public void DanglingLevelNumberBeforeASectionHeader_IsTheMissingPeriod(string entry, string diagnostic)
    {
        foreach (int edition in new[] { 85, 2002, 2014, 2023 })
        {
            var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       " + entry + "\n" + Proc, edition);
            Assert.True(errors.Count == 1, $"[{edition}] expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
            Assert.Contains("error " + diagnostic, errors[0]);
        }
    }

    /// <summary>⛔ A HEADER WRITTEN AFTER A LEVEL-NUMBER IS NEVER A NAME AND A CLAUSE (kb/Work PB4706). SECTION and DIVISION
    /// close a header, and no format follows a data-name with either. Where a period lets the header follow, the fault is
    /// that period (ISO §13.16.2 ends the entry with one), one report, with no cascade from the header it used to swallow:
    /// the second-report rows prove the recovery resumes in the procedure division. Where none does, the report is the plain
    /// one at the word it fails, as after a complete entry. PROCEDURE is reserved at every edition (§8.3.2.1 1)), so it
    /// names no entry whatever follows it.</summary>
    [Theory]
    [InlineData("01\n" + Proc, 85, "COBOL0307: unexpected 'PROCEDURE'. A period may be missing")]
    [InlineData("01\n" + Proc, 2023, "COBOL0307: unexpected 'PROCEDURE'. A period may be missing")]
    [InlineData("77\n" + Proc, 2023, "COBOL0307: unexpected 'PROCEDURE'. A period may be missing")]
    [InlineData("01 W-A PIC X.\n       88\n" + Proc, 2023, "COBOL0307: unexpected 'PROCEDURE'. A period may be missing")]
    [InlineData("01 LOCAL-STORAGE SECTION.\n" + Proc, 2002, "COBOL0307: unexpected 'LOCAL-STORAGE'. A period may be missing")]
    [InlineData("01 LOCAL-STORAGE SECTION.\n" + Proc, 2023, "COBOL0307: unexpected 'LOCAL-STORAGE'. A period may be missing")]
    [InlineData("01 SCREEN SECTION.\n" + Proc, 2023, "COBOL0307: unexpected 'SCREEN'. A period may be missing")]
    [InlineData("01 W-A\n       LOCAL-STORAGE SECTION.\n" + Proc, 2023, "COBOL0307: cannot parse construct near 'LOCAL-STORAGE'. A period may be missing")]
    [InlineData("01 W-A PIC X\n       LOCAL-STORAGE SECTION.\n" + Proc, 2023, "COBOL0307: missing token before 'LOCAL-STORAGE'. A period may be missing")]
    [InlineData("01 LOCAL-STORAGE SECTION.\n" + Proc, 85, "COBOL0001: cannot parse construct near 'SECTION'")]
    [InlineData("01 SCREEN SECTION.\n" + Proc, 85, "COBOL0001: cannot parse construct near 'SECTION'")]
    [InlineData("01 FILE SECTION.\n" + Proc, 2023, "COBOL0001: unexpected 'FILE'")]
    [InlineData("01 WORKING-STORAGE SECTION.\n" + Proc, 2023, "COBOL0001: unexpected 'WORKING-STORAGE'")]
    [InlineData("01 DATA DIVISION.\n" + Proc, 2023, "COBOL0001: unexpected 'DATA'")]
    [InlineData("01 MOVE SECTION.\n" + Proc, 2023, "COBOL0001: unexpected 'MOVE'")]
    [InlineData("01\n       MAIN SECTION.\n" + Proc, 2023, "COBOL0001: cannot parse construct near 'SECTION'")]
    [InlineData("01 FOO DIVISION.\n" + Proc, 2023, "COBOL0001: cannot parse construct near 'DIVISION'")]
    [InlineData("01 W-A\n       MAIN SECTION.\n" + Proc, 2023, "COBOL0001: unexpected 'SECTION'")]
    [InlineData("01 W-A PIC X\n       MAIN SECTION.\n" + Proc, 2023, "COBOL0001: unexpected 'SECTION'")]
    [InlineData("01 W-A PIC X SECTION.\n" + Proc, 2023, "COBOL0001: unexpected 'SECTION'")]
    [InlineData("01 PROCEDURE PIC X.\n" + Proc, 85, "COBOL0901: 'PROCEDURE' is a reserved word in COBOL-85")]
    [InlineData("01 PROCEDURE PIC X.\n" + Proc, 2023, "COBOL0901: 'PROCEDURE' is a reserved word in COBOL-2023")]
    [InlineData("01 PROCEDURE.\n" + Proc, 2023, "COBOL0901: 'PROCEDURE' is a reserved word in COBOL-2023")]
    [InlineData("01 PROCEDURE MOVE.\n" + Proc, 2023, "COBOL0901: 'PROCEDURE' is a reserved word in COBOL-2023")]
    public void AHeaderAfterALevelNumber_IsNeverANameAndAClause(string rest, int edition, string diagnostic)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       " + rest, edition);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error " + diagnostic, errors[0]);
    }

    /// <summary>⛔ A FOREIGN RUN THAT A HEADER ENDS IS STILL NAMED (kb/Work PB4770, PB4301): the run begins BEFORE the header's
    /// opening word, so the header's own name is not what the run is made of; only a run that IS the header's name
    /// (<c>PIC X MAIN SECTION.</c>, above) is a structure fault.</summary>
    [Theory]
    [InlineData("01 W-A PIC X VALEU MAIN SECTION.\n" + Proc, "COBOL1941: unexpected 'VALEU'. 'VALEU' is not a clause of the data description entry for 'W-A'")]
    [InlineData("01 W-A PIC X VALEU 5 DIVISION.\n" + Proc, "COBOL1941: unexpected 'VALEU'. 'VALEU' is not a clause of the data description entry for 'W-A'")]
    public void AForeignRunEndedByAHeader_IsNamed(string rest, string diagnostic)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       " + rest, 2023);
        Assert.Contains("error " + diagnostic, errors[0]);
    }

    /// <summary>⛔ THE FAULT AFTER THE HEADER DOES NOT UNWRITE THE PERIOD BEFORE IT (kb/Work PB4706, the PB4646 review's R1-1): the
    /// slot and hint 14 ask one question of the word and the one after it, so a word that begins what follows gets the
    /// missing-period report whatever else its sentence holds, and that fault is the second report. With a name or a clause
    /// before the header the sentence's reading ends at the header's closing word, the same question.</summary>
    [Theory]
    [InlineData("01 LINKAGE SECTION FOO.", "COBOL0307: unexpected 'LINKAGE'. A period may be missing", "COBOL0001: unexpected 'FOO'")]
    [InlineData("01 END PROGRAM PB2533U FOO.", "COBOL0307: unexpected 'END'. A period may be missing", "COBOL0001: unexpected 'FOO'")]
    [InlineData("01 W-A PIC X\n       LINKAGE SECTION FOO.", "COBOL0307: missing token before 'LINKAGE'. A period may be missing", "COBOL0001: unexpected 'FOO'")]
    public void AFaultAfterTheHeader_IsTheSecondReport(string rest, string first, string second)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       " + rest + "\n" + Proc);
        Assert.True(errors.Count >= 2, $"expected the period's report and the fault after it, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error " + first, errors[0]);
        Assert.Contains("error " + second, errors[1]);
    }

    /// <summary>The header's words at the other slots that take a data-name (kb/Work PB4706): the report group and constant
    /// entries of the report and screen sections, whose PROCEDURE alternative cascaded through the lost header.</summary>
    [Theory]
    [InlineData("SCREEN SECTION.\n       01")]
    [InlineData("REPORT SECTION.\n       RD R.\n       01")]
    [InlineData("REPORT SECTION.\n       RD R.\n       01 G")]
    public void ADanglingLevelNumberOfAnotherSection_IsTheMissingPeriodToo(string section)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       " + section + "\n" + Proc);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error COBOL0307: ", errors[0]);
        Assert.Contains("'PROCEDURE'. A period may be missing", errors[0]);
    }

    /// <summary>⛔ A HEADER OUT OF ORDER AFTER A DANGLING LEVEL-NUMBER IS THE SAME REPORT AT EVERY ENTRY SLOT (kb/Work PB4788,
    /// PB4706). A section's entry loop fails its prediction at the header's first word with the parser still before the
    /// level-number, so no entry context exists; the slot is read from the walk (<c>OffendingPosition.FollowsALevelNumber</c>).
    /// §13.2.1 prints the LINKAGE SECTION before the REPORT SECTION and the SCREEN SECTION last, so neither section can be
    /// followed by LINKAGE SECTION (the screen section by REPORT SECTION either): no period lets the header follow, and the word is no kind "expected" at the
    /// slot (it drew COBOL5506 listing every clause), the plain report <c>01 FILE SECTION.</c> draws in the working-storage
    /// section.</summary>
    [Theory]
    [InlineData("SCREEN SECTION.\n       01 LINKAGE SECTION.", "COBOL0001: unexpected 'LINKAGE'")]
    [InlineData("SCREEN SECTION.\n       01 S1 LINE 1 COL 1 VALUE \"X\".\n       01 LINKAGE SECTION.", "COBOL0001: unexpected 'LINKAGE'")]
    [InlineData("SCREEN SECTION.\n       01 K CONSTANT AS 5.\n       01 REPORT SECTION.", "COBOL0001: unexpected 'REPORT'")]
    [InlineData("REPORT SECTION.\n       RD R.\n       01 LINKAGE SECTION.", "COBOL0001: unexpected 'LINKAGE'")]
    public void AnOutOfOrderHeaderAfterADanglingLevelNumber_IsNoKindExpected(string section, string diagnostic)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       " + section + "\n" + Proc);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains("error " + diagnostic, errors[0]);
        Assert.DoesNotContain("COBOL5506", errors[0]);
    }

    /// <summary>⛔ A SECTION'S ENTRY LOOP NAMES NO ENTRY AFTER A HEADER'S FIRST WORD (kb/Work PB4818). The report section's loop
    /// chooses between its two entry kinds before the level-number, where adaptive prediction reads
    /// <c>noHeaderWrittenHere</c> as true, so it took a gated word as the name and died at SECTION. Each line is the report the
    /// same words draw in the working-storage section where §8.9 reserves the word and §13.2.1 lets the header follow, the
    /// out-of-order report where it does not, and the plain report at SECTION where §8.9 frees the word (85), and each is one
    /// report, at whichever entry precedes the line and at any level-number.</summary>
    [Theory]
    [InlineData("01 SCREEN SECTION.", 2023, "(6,11): error COBOL0307: unexpected 'SCREEN'. A period may be missing")]
    [InlineData("01 SCREEN SECTION.", 2002, "(6,11): error COBOL0307: unexpected 'SCREEN'. A period may be missing")]
    [InlineData("02 SCREEN SECTION.", 2023, "(6,11): error COBOL0307: unexpected 'SCREEN'. A period may be missing")]
    [InlineData("01 A TYPE DE.\n       01 SCREEN SECTION.", 2023, "(7,11): error COBOL0307: unexpected 'SCREEN'. A period may be missing")]
    [InlineData("01 K CONSTANT AS 1.\n       01 SCREEN SECTION.", 2023, "(7,11): error COBOL0307: unexpected 'SCREEN'. A period may be missing")]
    [InlineData("01 LOCAL-STORAGE SECTION.", 2023, "(6,11): error COBOL0001: unexpected 'LOCAL-STORAGE'")]
    [InlineData("01 SCREEN DIVISION.", 2023, "(6,11): error COBOL0001: unexpected 'SCREEN'")]
    [InlineData("01 COMMUNICATION SECTION.", 2002, "(6,11): error COBOL0001: unexpected 'COMMUNICATION'")]
    [InlineData("01 SCREEN SECTION.", 85, "(6,18): error COBOL0001: cannot parse construct near 'SECTION'")]
    [InlineData("01 MAIN SECTION.", 2023, "(6,16): error COBOL0001: cannot parse construct near 'SECTION'")]
    public void AHeaderAfterAReportGroupsLevelNumber_IsReportedAtTheDataEntrysWord(string line, int edition, string diagnostic)
    {
        var errors = Compile(Head + "       DATA DIVISION.\n       REPORT SECTION.\n       RD R.\n       " + line + "\n" + Proc, edition);
        Assert.True(errors.Count == 1, $"expected exactly one diagnostic, got:\n{string.Join("\n", errors)}");
        Assert.Contains(diagnostic, errors[0]);
    }

    /// <summary>The legal readings the header rule leaves alone (kb/Work PB4706): a word that only STARTS like a header, a word
    /// §8.9 leaves free as an entry-name, and a gated word as an unnamed clause. NC205A's <c>77 PROCEDURE</c>, continued on its
    /// next line, is one word (the 1985 word continuation, which COBOL-2023 removed), not the reserved word.</summary>
    [Theory]
    [InlineData("77 PROCEDURE-DIVISION PIC X.", 2023)]
    [InlineData("01 SECTIONS PIC X.", 2023)]
    [InlineData("01 LOCAL-STORAGE PIC X.", 85)]
    [InlineData("01 SCREEN PIC X.", 85)]
    [InlineData("01 BINARY-LONG.", 2023)]
    [InlineData("77 PROCEDURE\n      -    DIVISION PIC X.", 85)]
    public void ANameThatWritesNoHeader_IsAccepted(string entry, int edition)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB4706_");
        try
        {
            string src = Path.Combine(dir, "pb4706u.cob");
            File.WriteAllText(src, Head + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n       " + entry + "\n" + Proc);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb4706u.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            Assert.True(r.Success, string.Join("\n", r.Errors));
        }
        finally { ScratchDirectory.Remove(dir); }
    }

    private static IReadOnlyList<string> Compile(string source, int edition = 2023)
    {
        string dir = ScratchDirectory.Create("WiseOwlCobol_PB2533_");
        try
        {
            string src = Path.Combine(dir, "pb2533u.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb2533u.dll"), DialectLevel: edition, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            Assert.False(r.Success, "the source is non-conforming and must be rejected");
            return r.Errors;
        }
        finally { ScratchDirectory.Remove(dir); }
    }
}
