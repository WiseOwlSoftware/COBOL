// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System.Text.RegularExpressions;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// The parse-error recovery hints say only what is TRUE (kb/Work PB2770). A hint keyed on a word's spelling used to win
/// the message wherever the parse happened to fail at that word, and each such hint claimed a limitation the compiler
/// does not have: <c>MOVE 1 SET X TO 2</c> (MOVE lacks its TO) answered "This SET form may not be supported",
/// <c>MOVE 1 SEARCH …</c> "SEARCH statement may not be fully supported", and <c>EVALUATE X ALSO ALSO X …</c>
/// "EVALUATE with ALSO (multi-subject) may not be fully supported". Those hints are deleted, so each row is refused
/// with no "may not be" claim. The misplaced-scope-terminator hint (ISO §14.5.3.2: an explicit scope terminator closes
/// the most recent unterminated statement having its statement-name) is keyed on the token TYPE and on the statement
/// rules the ATN says can match it: END-SEARCH inside SEARCH ALL has its matching statement, so the hint is silent; an
/// END-IF or END-SEARCH outside any IF or SEARCH names the statement it lacks; and a user-defined word spelled END-…
/// is no terminator, so it is never told it lacks a statement named after its spelling.
/// </summary>
public sealed class ParseRecoveryHintTruthTests
{
    [Theory]
    [InlineData("PBHT01", "MOVE 1 SET W-N TO 2")]
    [InlineData("PBHT02", "MOVE 1 SEARCH T-E WHEN T-E (IX) = \"A\" DISPLAY 1 END-SEARCH")]
    [InlineData("PBHT03", "EVALUATE W-N ALSO ALSO W-N WHEN 1 ALSO 2 DISPLAY 1 END-EVALUATE")]
    [InlineData("PBHT04", "SEARCH ALL T-E END-SEARCH")]
    [InlineData("PBHT05", "SEARCH T-E END-SEARCH")]
    [InlineData("PBHT06", "MOVE 1 END-FLAG")]
    public void ARefusedStatement_IsNeverToldItsConstructIsUnsupported_OrUnmatched(string pid, string statement)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' is outside its general format, and it compiled");
        EditionHarness.AssertNoDiagnostic(diagnostics, "may not be");
        EditionHarness.AssertNoDiagnostic(diagnostics, "COBOL0306");
    }

    /// <summary>
    /// A literal whose text spells a verb after a period is a literal, not a paragraph name run into a statement: no COBOL
    /// word can carry a period (ISO §8.3.2.1), so the deleted COBOL0304 hint, keyed on a token's text containing ".MOVE"
    /// and its siblings, could only ever fire on a literal or a PICTURE string, and there it was false.
    /// </summary>
    [Theory]
    [InlineData("PBHT21", "MOVE W-N \"A.MOVE\"")]
    [InlineData("PBHT22", "GO TO \"PARA.PERFORM\"")]
    public void ALiteralSpellingPeriodVerb_IsNeverToldAParagraphPeriodIsMissing(string pid, string statement)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' is outside its general format, and it compiled");
        EditionHarness.AssertNoDiagnostic(diagnostics, "COBOL0304");
        EditionHarness.AssertNoDiagnostic(diagnostics, "paragraph name");
        EditionHarness.AssertNoDiagnostic(diagnostics, "qualified reference");
    }

    /// <summary>An explicit scope terminator terminates "the most-recently preceding unterminated statement having the
    /// statement-name for which that scope terminator is defined" (ISO §14.5.3.2 1)), so one written where no such
    /// statement is unterminated draws COBOL0306 naming the statement it lacks, ONCE, on the fault's one report. The
    /// rows from PBHT13 are kb/Work PB3832's review R2-2: where the parse's prediction looked past the terminator (a
    /// statement follows it), the report reads the rules open at the terminator off a re-simulation of the parse, and
    /// that reading counted every statement rule a statement may BEGIN with there as open, and the IF a terminator had
    /// just completed (PBHT16), so the terminator found its statement and drew a bare COBOL0001.</summary>
    [Theory]
    [InlineData("PBHT11","MOVE 1 TO W-N END-IF", "END-IF appears without a matching IF statement.")]
    [InlineData("PBHT12", "MOVE 1 TO W-N END-SEARCH", "END-SEARCH appears without a matching SEARCH statement.")]
    [InlineData("PBHT13", "MOVE 1 TO W-N END-IF MOVE 2 TO W-N.", "END-IF appears without a matching IF statement.")]
    [InlineData("PBHT14", "MOVE 1 TO W-N END-IF.", "END-IF appears without a matching IF statement.")]
    [InlineData("PBHT15", "ADD 1 TO W-N ON SIZE ERROR DISPLAY \"X\" END-IF.", "END-IF appears without a matching IF statement.")]
    [InlineData("PBHT16", "IF W-N = 0 IF W-N = 1 DISPLAY \"A\" END-IF END-IF END-IF.", "END-IF appears without a matching IF statement.")]
    [InlineData("PBHT17", "DISPLAY \"A\" END-PERFORM DISPLAY \"B\".", "END-PERFORM appears without a matching PERFORM statement.")]
    [InlineData("PBHT18", "IF W-N = 0 MOVE 1 TO W-N END-ADD MOVE 2 TO W-N.", "END-ADD appears without a matching ADD statement.")]
    public void AScopeTerminatorOutsideItsStatement_NamesTheStatement(string pid, string statement, string message)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' closes a statement that is not open, and it compiled");
        Assert.Equal(["COBOL0306"], Codes(diagnostics));
        EditionHarness.AssertHasDiagnostic(diagnostics, message);
    }

    /// <summary>A sentence's last statement "is terminated by a separator period" (ISO §14.5.1), so a section or
    /// paragraph header written after a statement with no period between draws COBOL0307 "A period may be missing",
    /// once, on the fault's FIRST report, at depth 0 and inside an open statement alike (kb/Work PB3832, review R2-1).
    /// The parse fails there as a failed prediction, whose report carried no expected set, so the hint spoke only on
    /// the recovery's repeat at the same token, and the one-report-per-fault rule drops that repeat: the commonest
    /// COBOL syntax error drew a bare COBOL0001. The rows pin that path: inside a section, the header fails a
    /// prediction that looked past it ("cannot parse construct near").</summary>
    [Theory]
    [InlineData("PBHT31", "MOVE 1 TO W-N", "S2 SECTION.", "SECTION")]
    [InlineData("PBHT32", "DISPLAY \"X\"", "S2 SECTION.", "SECTION")]
    [InlineData("PBHT33", "ADD 1 TO W-N", "S2 SECTION.", "SECTION")]
    [InlineData("PBHT34", "COMPUTE W-N = W-N + 1", "P2.", "P2")]
    [InlineData("PBHT35", "STOP RUN", "P2.", "P2")]
    [InlineData("PBHT36", "IF W-N = 0 MOVE 1 TO W-N", "S2 SECTION.", "SECTION")]
    [InlineData("PBHT37", "IF W-N = 0 DISPLAY \"A\" ELSE IF W-N = 2 DISPLAY \"B\" END-IF DISPLAY \"C\"", "S2 SECTION.", "SECTION")]
    // a context-sensitive word is a legal paragraph-name, so this header IS the next sentence (kb/Work PB906): the
    // negative of PBHT42, whose IGNORING is followed by a LOCK no paragraph header takes
    [InlineData("PBHT38", "CONTINUE", "IGNORING.", "IGNORING")]
    public void AMissingSeparatorPeriod_IsNamedOnTheFaultsOneReport(string pid, string statement, string header, string near)
    {
        var (ok, diagnostics) = EditionHarness.Compile($"""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. {pid}.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 W-N PIC 9(4) VALUE 0.
                   PROCEDURE DIVISION.
                   S1 SECTION.
                   P1.
                       {statement}
                   {header}
                       STOP RUN.

            """, 2023);
        Assert.False(ok, $"{pid}: '{statement}' lacks the period that ends its sentence, and it compiled");
        Assert.Equal(["COBOL0307"], Codes(diagnostics));
        EditionHarness.AssertHasDiagnostic(diagnostics, $"cannot parse construct near '{near}'. A period may be missing");
    }

    /// <summary>A COMPLETE statement followed by a word its general formats do not print there is that statement written
    /// with an unprinted phrase, a bracket's second alternative, a repeated phrase or a phrase out of its sequence
    /// (ISO §5.2.1), and the diagnostic says so: COBOL2269 naming the statement, never "A period may be missing" (kb/Work
    /// PB906). The hint blamed punctuation wherever the word could ALSO be a paragraph-name (<c>WRITE R IGNORING
    /// LOCK.</c>: <c>. IGNORING</c> parses, and only the LOCK after it fails), so the period must let the whole next
    /// sentence follow before it is called missing. The statement named is the one whose own syntax prints the word
    /// (PBHT44: the IF of a second ELSE, not the DISPLAY before it), and a SET format is named SET (PBHT45).</summary>
    [Theory]
    [InlineData("PBHT41", "MOVE 1 TO W-N ROUNDED", "MOVE", "ROUNDED")]
    [InlineData("PBHT42", "CONTINUE IGNORING LOCK", "CONTINUE", "IGNORING")]
    [InlineData("PBHT43", "ADD 1 TO W-N ROUNDED ROUNDED", "ADD", "ROUNDED")]
    [InlineData("PBHT44", "IF W-N = 1 DISPLAY \"A\" ELSE DISPLAY \"B\" ELSE DISPLAY \"C\" END-IF", "IF", "ELSE")]
    [InlineData("PBHT45", "SET IX UP BY 1 DOWN BY 1", "SET", "DOWN")]
    [InlineData("PBHT46", "IF W-N = 1 MOVE 1 TO W-N ROUNDED END-IF", "MOVE", "ROUNDED")]
    public void AStatementFollowedByAPhraseItsFormatDoesNotPrint_NamesTheStatement_NotAMissingPeriod(
        string pid, string statement, string named, string near)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Program(pid, statement), 2023);
        Assert.False(ok, $"{pid}: '{statement}' writes a phrase its format does not print there, and it compiled");
        Assert.Equal("COBOL2269", Codes(diagnostics).First());
        EditionHarness.AssertHasDiagnostic(diagnostics, $"unexpected '{near}'. The {named} statement ends before this token: "
            + $"no general format of {named} prints it after what is written");
        EditionHarness.AssertNoDiagnostic(diagnostics, "A period may be missing");
    }

    private static string[] Codes(IEnumerable<string> diagnostics) =>
        [.. diagnostics.SelectMany(d => Regex.Matches(d, @"\bCOBOL\d{4}\b").Select(m => m.Value))];

    private static string Program(string pid, string statement) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 W-N PIC 9(4) VALUE 0.
        01 END-FLAG PIC 9.
        01 T.
           05 T-E PIC X OCCURS 5 ASCENDING KEY T-E INDEXED BY IX.
        PROCEDURE DIVISION.
        MAIN-PARA.
            {statement}
            STOP RUN.

        """;
}
