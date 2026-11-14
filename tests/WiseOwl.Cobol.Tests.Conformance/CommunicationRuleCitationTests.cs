// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A COMMUNICATION STATEMENT'S DIAGNOSTIC QUOTES THE RULE OF THE EDITION THE PROGRAM TARGETS (kb/Work PB5343). X3.23-1985's §1985:XIV.3 and ISO/IEC
/// 1989:2002's Annex A.2 state the same syntax rules under different numbers and words: 2002 inserted SEND's SR2 ("Identifier-1 shall reference a data item of
/// class alphanumeric"), so SEND's 85 SR2 to SR6 are its SR3 to SR7, and 85 writes "must" where 2002 writes "shall". The COBOL6170 and COBOL6171 messages
/// quoted 2002's A.2 rules at <c>--std 85</c>, naming an edition the user did not select. One program breaks each rule once; at 85 every message cites
/// §1985:XIV.3 and none cites 2002, and at 2002 the reverse. The conformance goldens pin only the code, so the wording is pinned here.
/// </summary>
public sealed class CommunicationRuleCitationTests
{
    private const string Program = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB5343C.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01  MSG PIC X(10) VALUE "HELLO".
               01  N2 PIC 99 VALUE 12.
               COMMUNICATION SECTION.
               CD  CIN FOR INPUT
                   SYMBOLIC QUEUE IS IN-Q
                   STATUS KEY IS IN-STAT.
               CD  COUT FOR OUTPUT
                   DESTINATION COUNT IS OUT-CNT
                   STATUS KEY IS OUT-STAT
                   SYMBOLIC DESTINATION IS OUT-DEST.
               PROCEDURE DIVISION.
               P-PARA.
                   SEND CIN FROM MSG WITH EMI.
                   SEND COUT FROM MSG WITH N2.
                   SEND COUT FROM MSG WITH EMI AFTER ADVANCING MSG LINES.
                   SEND COUT FROM MSG WITH EMI AFTER ADVANCING 1.5 LINES.
                   ENABLE OUTPUT CIN.
                   ACCEPT COUT MESSAGE COUNT.
                   STOP RUN.
        """;

    public static TheoryData<int, string> Citations() => new()
    {
        { 85, "SEND: 'CIN' is Format 1, an input CD; §1985:XIV.3.6.3 SR1: \"Cd-name-1 must reference an output CD or input-output CD.\"" },
        { 85, "SEND WITH N2: §1985:XIV.3.6.3 SR2: \"Identifier-2 must reference a one-character integer without an operational sign.\"" },
        { 85, "SEND ADVANCING MSG: §1985:XIV.3.6.3 SR3: \"Identifier-3 must reference an integer data item.\"" },
        { 85, "SEND ADVANCING 1.5: §1985:XIV.3.6.2 prints integer-1 here, and §1985:XIV.3.6.3 SR5:" },
        { 85, "§1985:XIV.3.3.3 SR3: \"Cd-name-1 must reference an output CD when the OUTPUT phrase is specified.\"" },
        { 85, "§1985:XIV.3.1.3 SR1: \"Cd-name-1 must reference an input CD.\"" },
        { 2002, "SEND: 'CIN' is Format 1, an input CD; §2002:A.2.6.2 SR1: \"Cd-name-1 shall reference an output CD or input-output CD.\"" },
        { 2002, "SEND WITH N2: §2002:A.2.6.2 SR3: \"Identifier-2 shall reference a one-character integer without an operational sign.\"" },
        { 2002, "SEND ADVANCING MSG: §2002:A.2.6.2 SR4: \"Identifier-3 shall reference an integer data item.\"" },
        { 2002, "SEND ADVANCING 1.5: §2002:A.2.6.1 prints integer-1 here, and §2002:A.2.6.2 SR6:" },
        { 2002, "§2002:A.2.3.2 SR3: \"Cd-name-1 shall reference an output CD when the OUTPUT phrase is specified.\"" },
        { 2002, "§2002:A.2.1.2 SR1: \"Cd-name-1 shall reference an input CD.\"" },
    };

    [Theory]
    [MemberData(nameof(Citations))]
    public void EachRefusal_QuotesTheTargetedEditionsRule(int edition, string expected)
    {
        var (ok, errors, _) = EditionHarness.CompileFull(Program, edition);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, expected);
    }

    [Theory]
    [InlineData(85, "1989:2002")]
    [InlineData(85, "§2002:")]
    [InlineData(2002, "§1985:")]
    public void NoRefusal_QuotesTheOtherEdition(int edition, string other)
    {
        var (_, errors, _) = EditionHarness.CompileFull(Program, edition);
        EditionHarness.AssertNoDiagnostic(errors.Where(e => e.Contains("COBOL617", StringComparison.Ordinal)), other);
    }
}
