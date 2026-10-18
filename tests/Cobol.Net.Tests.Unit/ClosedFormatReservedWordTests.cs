// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Frontend.Preprocessor;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// ⛔ A RESERVED WORD IN A CLOSED GENERAL FORMAT IS REFUSED BY NAME (kb/Work PB2533). Every closed format ends its
/// alternative list in the <c>unrecognizedClause</c> error production, which <c>ClosedFormatPass</c> refuses naming the
/// format (COBOLNET1941 / 1970 / 1971) — but the production begins at an IDENTIFIER, so a reserved word the format does
/// not print (<c>SD SF GLOBAL.</c>) never entered it and drew a bare parse error. <c>CobolErrorStrategy</c> now finds
/// the production's position in the ATN and reports the pass's own refusal, from the same <c>ClosedFormats</c> row.
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
        // §12.3.2 — the configuration section's PARAGRAPH list.
        { "configuration-section", Head + "       ENVIRONMENT DIVISION.\n       CONFIGURATION SECTION.\n       GLOBAL.\n" + Proc,
          "COBOLNET1971", "'GLOBAL' is not a paragraph of the CONFIGURATION SECTION" },
        // §11.2.1 — the identification division's PARAGRAPH list.
        { "identification", Head + "       GLOBAL.\n" + Proc,
          "COBOLNET1971", "'GLOBAL' is not a paragraph of the IDENTIFICATION DIVISION" },
    };

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

    private static IReadOnlyList<string> Compile(string source)
    {
        string dir = Path.Combine(Path.GetTempPath(), "CobolNet_PB2533_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        try
        {
            string src = Path.Combine(dir, "pb2533u.cob");
            File.WriteAllText(src, source);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(
                src, Path.Combine(dir, "pb2533u.dll"), DialectLevel: 2023, CheckOnly: true, SourceFormat: InitialReferenceFormat.Fixed));
            Assert.False(r.Success, "the source is non-conforming and must be rejected");
            return r.Errors;
        }
        finally { try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ } }
    }
}
