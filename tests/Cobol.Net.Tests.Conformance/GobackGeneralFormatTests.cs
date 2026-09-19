// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ §14.9.18.2's GENERAL FORMAT, READ FROM THE PRINTED FIGURE — the spellings the grammar owes, and the two
/// controls that prove the relaxation did not simply open the rule up (kb/Work PB407).
///
/// <para><b>The three facts, measured on the page</b> (PDF page 661 / printed folio 631 and PDF page 653 /
/// printed folio 623, rendered at 300 dpi):
/// <list type="number">
///   <item>The tail bracket carries CHOICE INDICATORS — the `|` bars just inside `⎡ … ⎤`. §5.2.6.4: "When
///         enclosed by brackets, zero or more of the alternatives contained within the choice indicators shall
///         be specified, but any single alternative may be specified only once", and "The alternatives may be
///         specified in any order." The grammar said <c>(raisingPhrase | statusPhrase)?</c> — an ORDERED
///         at-most-one stack — so BOTH phrases together was a syntax error in either order.</item>
///   <item>`STATUS` is NOT underlined, so §5.2.3 makes it an OPTIONAL word; the grammar required it before an
///         operand, and `GOBACK WITH ERROR 5.` was <c>COBOL0001: no viable alternative at input '5'</c>. The
///         rule is SHARED with STOP (§14.9.42.2 prints the same underlining), so the defect was two verbs
///         wide.</item>
///   <item>In `LAST EXCEPTION` only `LAST` is underlined, so the trailing `EXCEPTION` is an optional word too —
///         on the GOBACK figure AND on EXIT Format 2, which shares the grammar's one <c>raisingPhrase</c> rule.
///         §7.3.21.4 rule 2 writes the phrase back as "as though a GOBACK RAISING LAST statement were
///         executed", which is the standard citing itself.</item>
/// </list></para>
///
/// <para>The "only once" half of §5.2.6.4 cannot be a parser rule without enumerating the orderings, so it is a
/// bind-time screen (COBOLNET2104) and is asserted here beside the spellings it bounds.</para>
/// </summary>
public sealed class GobackGeneralFormatTests
{
    private const string ChoiceRepeated = "COBOLNET2104";

    /// <summary>A main program whose one paragraph is <paramref name="body"/>. The PD header carries
    /// RAISING EC-USER-X so an `EXCEPTION EC-USER-X` operand satisfies §14.9.18.3 SR2's second sentence.</summary>
    private static string Prog(string pid, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        PROCEDURE DIVISION RAISING EC-USER-X.
        MAIN-PARA.
            {body}.

        """;

    /// <summary>A program whose declarative paragraph is <paramref name="body"/> — §14.9.18.3 SR5 / §14.9.14.3
    /// SR6 admit the LAST phrase there, so a RAISING LAST spelling can be tested for its SYNTAX alone.</summary>
    private static string InDeclarative(string pid, string body) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        PROCEDURE DIVISION.
        DECLARATIVES.
        D-SEC SECTION. USE AFTER EXCEPTION CONDITION EC-SIZE.
        D-PARA.
            {body}.
        END DECLARATIVES.
        MAIN-SEC SECTION.
        MAIN-PARA.
            STOP RUN.

        """;

    /// <summary>Fact 1 — both phrases, in EITHER order, and each alone; plus the two spellings that were already
    /// accepted, so a later "simplify the tail" edit that re-collapses the alternation fails here.</summary>
    [Theory]
    [InlineData("PBFM01", "GOBACK RAISING EXCEPTION EC-USER-X WITH ERROR STATUS 5")]
    [InlineData("PBFM02", "GOBACK WITH ERROR STATUS 5 RAISING EXCEPTION EC-USER-X")]
    [InlineData("PBFM03", "GOBACK RAISING EXCEPTION EC-USER-X")]
    [InlineData("PBFM04", "GOBACK WITH ERROR STATUS 5")]
    [InlineData("PBFM05", "GOBACK")]
    // Fact 2 — STATUS omitted before the operand, and the already-legal bare keyword control beside it.
    [InlineData("PBFM06", "GOBACK WITH ERROR 5")]
    [InlineData("PBFM07", "GOBACK NORMAL 7")]
    [InlineData("PBFM08", "GOBACK ERROR")]
    public void EverySpellingTheFormatAdmits_Compiles(string pid, string body)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Prog(pid, body), 2023);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>Fact 2, THE OTHER VERB. <c>statusPhrase</c> is ONE grammar rule shared by GOBACK and STOP
    /// (§14.9.42.2 / §14.9.18.2 print the same un-underlined WITH and STATUS), so the required-STATUS defect was
    /// two verbs wide and the repair is inherited rather than repeated — this row is what holds that true.</summary>
    [Theory]
    [InlineData("PBFM09", "STOP RUN WITH ERROR 5")]
    [InlineData("PBFM10", "STOP RUN WITH ERROR STATUS 5")]
    [InlineData("PBFM11", "STOP RUN ERROR")]
    [InlineData("PBFM12", "STOP RUN")]
    public void TheSharedStatusPhrase_ReadsTheSameOnStopRun(string pid, string body)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Prog(pid, body), 2023);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>Fact 3 — the trailing EXCEPTION is optional, on BOTH statements that share
    /// <c>raisingPhrase</c>, written in a declarative so only the SYNTAX is under test.</summary>
    [Theory]
    [InlineData("PBFM13", "GOBACK RAISING LAST")]
    [InlineData("PBFM14", "GOBACK RAISING LAST EXCEPTION")]
    [InlineData("PBFM15", "EXIT PROGRAM RAISING LAST")]
    [InlineData("PBFM16", "EXIT PROGRAM RAISING LAST EXCEPTION")]
    public void TheWordEXCEPTIONAfterLAST_IsOptionalOnBothVerbs(string pid, string body)
    {
        var (ok, diagnostics) = EditionHarness.Compile(InDeclarative(pid, body), 2023);
        Assert.True(ok, string.Join("\n", diagnostics));
    }

    /// <summary>§5.2.6.4's "only once" half — the part a parser rule cannot state. Relaxing the tail to a
    /// repetition without this screen would ACCEPT a doubled phrase, which is the opposite error.</summary>
    [Theory]
    [InlineData("PBFM17", "GOBACK RAISING EXCEPTION EC-USER-X RAISING EXCEPTION EC-USER-X")]
    [InlineData("PBFM18", "GOBACK WITH ERROR STATUS 5 WITH NORMAL STATUS 6")]
    public void AnAlternativeWrittenTwice_IsRejected(string pid, string body)
        => EditionHarness.AssertHasDiagnostic(EditionHarness.GetDiagnostics(Prog(pid, body), 2023), ChoiceRepeated);

    /// <summary>⛔ THE EDITION GATE STILL READS THE PHRASE THAT WAS WRITTEN. Turning the tail into a repetition
    /// turns ANTLR's accessor into an ARRAY, which is never null — and the COBOL-2023 status gate tested
    /// <c>is not null</c>, so for one build a BARE <c>GOBACK.</c> at --std 2002 drew COBOLNET0900 naming a
    /// phrase the program did not contain. A grammar cardinality change is an API change at every reader.</summary>
    [Theory]
    [InlineData("PBFM19", "GOBACK", 2002, true)]
    [InlineData("PBFM20", "GOBACK", 2014, true)]
    [InlineData("PBFM21", "GOBACK WITH ERROR STATUS 5", 2014, false)]
    [InlineData("PBFM22", "GOBACK WITH ERROR 5", 2014, false)]
    public void TheStatusPhrasesEditionGate_FiresOnlyWhenThePhraseIsWritten(
        string pid, string body, int edition, bool expectOk)
    {
        var (ok, diagnostics) = EditionHarness.Compile(Prog(pid, body), edition);
        if (expectOk) Assert.True(ok, string.Join("\n", diagnostics));
        else EditionHarness.AssertHasDiagnostic(diagnostics, "COBOLNET0900");
    }
}
