// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A WRITTEN SEND HAS ONE FORMAT, AND THE BINDER ASSIGNS IT ONCE (kb/Work PB4090, PB4085).
/// <para>ISO §14.9.38.2 prints Format 1 (to-message-server) as <c>SEND TO { literal-1 | message-server-name-1 } FROM
/// identifier-1 RETURNING data-name-1</c> and Format 2 (message-server-response) as <c>SEND TO data-name-2 FROM
/// identifier-1 [ RAISING … ]</c>. The name after TO is a bare word in both, so which format a statement is depends on
/// its phrases AND on what the name IS: a message-tag data item (§14.9.38.3 SR4) is Format 2's, any other name is a
/// message-server-name-1 (SR2). <c>DeclinedFacilityBinder.ClassifySend</c> reads them in one place. The conformance
/// cases (<c>conformance-suite/wiseowl/cases/14.9.38-SEND/pb4090-*</c>) pin the refusals through the harness; this
/// class pins the part a golden cannot — the ABSENCE of a diagnostic — and the MESSAGE-TAG arm, whose item is refused
/// by name (COBOL1943, the data half of the declined facility) and so cannot sit in an accepted program.</para>
/// </summary>
public sealed class SendFormatClassificationTests
{
    private const string FormatRequiredPhraseOmitted = "COBOL1850";
    private const string PhrasesOfTwoFormats = "COBOL5488";
    private const string MessageTagUsageRefused = "COBOL1943";
    private const string Undefined = "COBOL1639";
    private const string NotANameShape = "COBOL2024";

    /// <summary>A main program with ordinary items and, when asked, a message-tag item <c>MT</c>; <paramref name="send"/> is the
    /// SEND statement (the caller wraps long ones across lines inside fixed-form margin R).</summary>
    private static string Prog(string pid, string send, bool messageTag = false) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {pid}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 WS-MSG PIC X(8) VALUE "MSG".
               01 WS-REPLY PIC X(8) VALUE "NONE".
               01 WS-TAG PIC X(8) VALUE "TAG".
               01 WS-LEN PIC 9(4) VALUE 0.
               01 GRP-B.
                  05 FLD PIC X(8).
               01 GRP.
                  05 TBL PIC X(8) OCCURS 3.
               {(messageTag ? "01 MT USAGE MESSAGE-TAG." : "")}
               PROCEDURE DIVISION.
               MAIN-P.
                   {send}
                   STOP RUN.

        """;

    /// <summary>A message-tag TO name is Format 2's (SR4), so it needs no RETURNING: the only diagnostic the program
    /// draws is the refusal of the item itself. Before the format was classified, a name was never asked.</summary>
    [Theory]
    [InlineData("PB4090A1", "SEND TO MT FROM WS-MSG END-SEND.")]
    [InlineData("PB4090A2", "SEND MT FROM WS-MSG END-SEND.")]
    [InlineData("PB4090A3", "SEND TO MT FROM WS-MSG\n                       RAISING LAST EXCEPTION END-SEND.")]
    public void AMessageTagToName_IsFormat2_AndNeedsNoReturning(string pid, string send)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog(pid, send, messageTag: true), 2023);
        EditionHarness.AssertHasDiagnostic(diagnostics, MessageTagUsageRefused);
        EditionHarness.AssertNoDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
        EditionHarness.AssertNoDiagnostic(diagnostics, PhrasesOfTwoFormats);
        EditionHarness.AssertNoDiagnostic(diagnostics, Undefined);
    }

    /// <summary>A name after TO that is no message-tag item is a message-server-name-1 (SR2): Format 1, whose RETURNING
    /// line is unbracketed. A name nothing declares is the same — a server name, never COBOL1639 — and so is a
    /// declared PIC item, because SR4's "shall be a message-tag data item" is a rule about data-name-2 and this
    /// statement has none.</summary>
    [Theory]
    [InlineData("PB4090B1", "SEND TO SRVNAME FROM WS-MSG END-SEND.", true)]
    [InlineData("PB4090B2", "SEND SRVNAME FROM WS-MSG END-SEND.", true)]
    [InlineData("PB4090B3", "SEND TO WS-TAG FROM WS-MSG END-SEND.", true)]
    [InlineData("PB4090B4", "SEND TO SRVNAME FROM WS-MSG RETURNING WS-REPLY END-SEND.", false)]
    [InlineData("PB4090B5", "SEND SRVNAME FROM WS-MSG RETURNING WS-REPLY END-SEND.", false)]
    [InlineData("PB4090B6", "SEND TO WS-TAG FROM WS-MSG RETURNING WS-REPLY END-SEND.", false)]
    [InlineData("PB4090B7", "SEND TO \"SRV\" FROM WS-MSG END-SEND.", true)]
    public void ANonTagToName_IsFormat1_AndNeedsReturning(string pid, string send, bool refused)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog(pid, send), 2023);
        EditionHarness.AssertNoDiagnostic(diagnostics, Undefined);
        EditionHarness.AssertNoDiagnostic(diagnostics, PhrasesOfTwoFormats);
        if (refused) EditionHarness.AssertHasDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
        else EditionHarness.AssertNoDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
    }

    /// <summary>A phrase of each format in one statement is in neither format: COBOL5488, once, and no cascade — a
    /// statement already refused for its mixture is not also asked for the RETURNING it may not have.</summary>
    [Theory]
    [InlineData("PB4090C1", "SEND TO SRVNAME FROM WS-MSG\n                       RETURNING WS-REPLY RAISING LAST EXCEPTION END-SEND.")]
    [InlineData("PB4090C2", "SEND \"SRV\" FROM WS-MSG RAISING LAST EXCEPTION END-SEND.")]
    [InlineData("PB4090C3", "SEND TO MT FROM WS-MSG RETURNING WS-REPLY END-SEND.")]
    [InlineData("PB4090C4", "SEND \"SRV\" FROM WS-MSG\n                       RETURNING WS-REPLY RAISING LAST EXCEPTION END-SEND.")]
    public void APhraseOfEachFormat_IsRefusedOnce_WithoutACascade(string pid, string send)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog(pid, send, messageTag: send.Contains(" MT ")), 2023);
        EditionHarness.AssertHasDiagnostic(diagnostics, PhrasesOfTwoFormats);
        Assert.Single(diagnostics, d => d.Contains(PhrasesOfTwoFormats, System.StringComparison.Ordinal));
        EditionHarness.AssertNoDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
    }

    /// <summary>RAISING makes the statement Format 2, so its TO name is a data-name-2 and is a data reference: an
    /// undefined one is COBOL1639, a declared one resolves. The same name with RETURNING (Format 1) is a server name and
    /// is no error — the two arms of one dispatch.</summary>
    [Theory]
    [InlineData("PB4090D1", "SEND TO NOSUCH FROM WS-MSG\n                       RAISING LAST EXCEPTION END-SEND.", true)]
    [InlineData("PB4090D2", "SEND NOSUCH FROM WS-MSG\n                       RAISING LAST EXCEPTION END-SEND.", true)]
    [InlineData("PB4090D3", "SEND TO WS-TAG FROM WS-MSG\n                       RAISING LAST EXCEPTION END-SEND.", false)]
    [InlineData("PB4090D4", "SEND TO NOSUCH FROM WS-MSG RETURNING WS-REPLY END-SEND.", false)]
    public void AFormat2ToName_IsADataReference_AFormat1ToName_IsNot(string pid, string send, bool undefined)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog(pid, send), 2023);
        if (undefined) EditionHarness.AssertHasDiagnostic(diagnostics, Undefined);
        else EditionHarness.AssertNoDiagnostic(diagnostics, Undefined);
        EditionHarness.AssertNoDiagnostic(diagnostics, PhrasesOfTwoFormats);
        EditionHarness.AssertNoDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
    }

    /// <summary>⛔ A NAME OPERAND IS ASKED HOW IT WAS WRITTEN (kb/Work PB4118). The operand after TO is
    /// message-server-name-1 or data-name-2 (§14.9.38.2), RETURNING prints data-name-1 and RECEIVE prints data-name-1 and
    /// data-name-2 (§14.9.31.2): names, so a subscript, a reference modifier or a special register is refused
    /// (COBOL2024) whatever format the rest of the statement is. The refusal is ONE diagnostic: a refused TO operand
    /// says nothing about the format, so the statement is not also asked for the RETURNING it may not have (COBOL1850),
    /// and a refused name is not also resolved (COBOL1639).</summary>
    [Theory]
    [InlineData("PB4118A1", "SEND TO TBL(1) FROM WS-MSG END-SEND.")]
    [InlineData("PB4118A2", "SEND TO TBL(1) FROM WS-MSG RETURNING WS-REPLY END-SEND.")]
    [InlineData("PB4118A3", "SEND TO WS-REPLY(1:2) FROM WS-MSG END-SEND.")]
    [InlineData("PB4118A4", "SEND TO LINE-COUNTER FROM WS-MSG END-SEND.")]
    [InlineData("PB4118A5", "SEND TO TBL(1) FROM WS-MSG\n                       RAISING LAST EXCEPTION END-SEND.")]
    [InlineData("PB4118A6", "SEND TO SRV FROM WS-MSG RETURNING TBL(1) END-SEND.")]
    [InlineData("PB4118A7", "SEND TO SRV FROM WS-MSG RETURNING WS-REPLY(2:3) END-SEND.")]
    [InlineData("PB4118A8", "RECEIVE FROM TBL(1) GIVING WS-MSG WS-LEN END-RECEIVE.")]
    [InlineData("PB4118A9", "RECEIVE FROM WS-TAG GIVING WS-MSG TBL(1) END-RECEIVE.")]
    [InlineData("PB4118AA", "RECEIVE FROM WS-TAG(1:2) GIVING WS-MSG WS-LEN END-RECEIVE.")]
    public void ANameWrittenAsAnythingButAName_IsRefusedOnce_WithoutACascade(string pid, string statement)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog(pid, statement), 2023);
        Assert.Single(diagnostics, d => d.Contains(NotANameShape, System.StringComparison.Ordinal));
        EditionHarness.AssertNoDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
        EditionHarness.AssertNoDiagnostic(diagnostics, PhrasesOfTwoFormats);
        EditionHarness.AssertNoDiagnostic(diagnostics, Undefined);
    }

    /// <summary>The legal side of the same screen: a qualifier is a name's own form (§8.4.2.2.2 Format 1), identifier-1
    /// (FROM, GIVING) may be subscripted or reference-modified, and a bare name is untouched. A qualified TO name is a
    /// data-name-2, so the statement is Format 2 and needs no RETURNING.</summary>
    [Theory]
    [InlineData("PB4118B1", "SEND TO FLD OF GRP-B FROM WS-MSG END-SEND.")]
    [InlineData("PB4118B2", "SEND TO FLD IN GRP-B FROM WS-MSG RAISING LAST EXCEPTION END-SEND.")]
    [InlineData("PB4118B3", "SEND TO SRV FROM TBL(2) RETURNING WS-REPLY END-SEND.")]
    [InlineData("PB4118B4", "SEND TO SRV FROM WS-MSG(1:2) RETURNING WS-REPLY END-SEND.")]
    [InlineData("PB4118B5", "RECEIVE FROM WS-TAG GIVING TBL(3) WS-LEN END-RECEIVE.")]
    [InlineData("PB4118B6", "RECEIVE FROM WS-TAG GIVING WS-MSG(1:2) WS-LEN END-RECEIVE.")]
    public void ANameWithItsOwnQualifier_AndAnIdentifierWithItsSubscript_AreNotRefused(string pid, string statement)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog(pid, statement), 2023);
        EditionHarness.AssertNoDiagnostic(diagnostics, NotANameShape);
        EditionHarness.AssertNoDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
        EditionHarness.AssertNoDiagnostic(diagnostics, PhrasesOfTwoFormats);
        EditionHarness.AssertNoDiagnostic(diagnostics, Undefined);
    }

    /// <summary>Below 2023 the facility is not part of the edition (Annex E.3.2 item 1): the introduction gate answers
    /// before any format is asked, as for every SEND (kb/Work PB374).</summary>
    [Theory]
    [InlineData(85)]
    [InlineData(2002)]
    [InlineData(2014)]
    public void BelowTheFacilitysEdition_TheIntroductionGateAnswers_NotTheFormatScreens(int edition)
    {
        var diagnostics = EditionHarness.GetDiagnostics(Prog("PB4090E1", "SEND TO SRVNAME FROM WS-MSG END-SEND."), edition);
        EditionHarness.AssertHasDiagnostic(diagnostics, "COBOL0900");
        EditionHarness.AssertNoDiagnostic(diagnostics, FormatRequiredPhraseOmitted);
    }
}
