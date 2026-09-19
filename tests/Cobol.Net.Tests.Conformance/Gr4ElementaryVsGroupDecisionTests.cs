// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ <b>ISO §14.9.25.4 GR4's ELEMENTARY-VS-GROUP DECISION — ONE rule over BOTH operands — and its
/// no-conversion clause</b> (kb/Work PB430). GR4: "Any move in which the sending operand is either a literal or
/// an elementary item AND the receiving item is an elementary item is an elementary move", and "Any move that is
/// not an elementary move, and does not reference a variable-length group, is treated exactly as if it were an
/// alphanumeric to alphanumeric elementary move, except that THERE IS NO CONVERSION OF DATA FROM ONE FORM OF
/// INTERNAL REPRESENTATION TO ANOTHER."
///
/// <para><b>The decision is not purely structural, and the compiler answered it differently on the two sides.</b>
/// §13.18.45.4 GR2 — "When the THROUGH phrase is specified, data-name-1 defines an alphanumeric group item" —
/// makes a level-66 THROUGH alias a GROUP item, although it is modelled here as ONE composed elementary
/// alphanumeric view; §13.18.45.4 GR1 makes the alias WITHOUT THROUGH take the renamed item's attributes, so the
/// control arm is genuinely elementary. Every case below is asserted against its STRUCTURAL TWIN — the plain
/// group or plain elementary item that the rule makes it the same kind of thing as — so the test states an
/// equivalence the standard requires, not a rendering this implementation happens to produce.</para>
/// </summary>
public sealed class Gr4ElementaryVsGroupDecisionTests
{
    private static string Run(string src, int edition = 85)
    {
        var (ok, stdout, detail) = new CobolNetCompiler(edition).CompileAndRun(src);
        Assert.True(ok, detail);
        return stdout.Replace("\r\n", "\n").TrimEnd('\n');
    }

    private static string Prog(string pid, string ws, string proc) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {pid}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {ws}
        PROCEDURE DIVISION.
        MAIN.
            {proc}
            STOP RUN.
        """;

    /// <summary>A RENAMES … THROUGH alias SENDER is a group item (§13.18.45.4 GR2), so the move is not
    /// elementary and the receiver's EDITING — which §14.9.25.4 GR6 confines to valid elementary moves — does
    /// not apply: the characters are moved alphanumeric-to-alphanumeric and space-filled on the right. Its
    /// structural twin, a plain two-leaf group of the same content, must give the same answer.</summary>
    [Fact]
    public void Gr4_RenamesThroughSender_IsAGroupSender_LikeItsStructuralTwin()
    {
        string src = Prog("GR4REN1", """
            01 WS-SPAN.
               05 WS-A PIC X VALUE "4".
               05 WS-B PIC X VALUE "5".
               05 WS-C PIC X VALUE "Z".
            66 WS-THRU RENAMES WS-A THRU WS-B.
            01 WS-TWIN.
               05 WS-T1 PIC X VALUE "4".
               05 WS-T2 PIC X VALUE "5".
            01 WS-ED PIC ZZ9.
            """, """
            MOVE WS-THRU TO WS-ED.
            DISPLAY "THRU=[" WS-ED "]".
            MOVE WS-TWIN TO WS-ED.
            DISPLAY "TWIN=[" WS-ED "]".
            """);
        Assert.Equal("THRU=[45 ]\nTWIN=[45 ]", Run(src));
    }

    /// <summary>⛔ THE CONTROL, and it is what makes the fix a discrimination rather than a blanket:
    /// §13.18.45.4 GR1 — "When the THROUGH phrase is not specified, all of the data attributes of data-name-2
    /// become the data attributes of data-name-1" — so the alias of an ELEMENTARY item is elementary, the move
    /// IS an elementary move, and PIC ZZ9 edits the alphanumeric sender's unsigned integer value.</summary>
    [Fact]
    public void Gr4_RenamesWithoutThroughSender_StaysElementary()
    {
        string src = Prog("GR4REN2", """
            01 WS-FLAT.
               05 WS-D PIC X(2) VALUE "45".
            66 WS-NOTHRU RENAMES WS-D.
            01 WS-ED PIC ZZ9.
            """, """
            MOVE WS-NOTHRU TO WS-ED.
            DISPLAY "NOTHRU=[" WS-ED "]".
            MOVE WS-D TO WS-ED.
            DISPLAY "DIRECT=[" WS-ED "]".
            """);
        Assert.Equal("NOTHRU=[ 45]\nDIRECT=[ 45]", Run(src));
    }

    /// <summary>⛔ THE OTHER ARM OF THE SAME DISPATCH (feedback_two_arm_dispatch). GR4's first sentence needs
    /// BOTH operands elementary, so a THROUGH alias RECEIVER makes the move non-elementary too — and §14.9.25.4
    /// GR6 a) drops a signed sender's operational sign only in a valid ELEMENTARY move, so the alias receiver
    /// keeps the DISPLAY overpunch exactly as its structural twin does.</summary>
    [Fact]
    public void Gr4_RenamesThroughReceiver_IsAGroupReceiver_LikeItsStructuralTwin()
    {
        string src = Prog("GR4REN3", """
            01 WS-RCV.
               05 WS-RA PIC X(3) VALUE "---".
               05 WS-RB PIC X(2) VALUE "--".
            66 WS-RTHRU RENAMES WS-RA THRU WS-RB.
            01 WS-GRCV.
               05 WS-GA PIC X(3) VALUE "---".
               05 WS-GB PIC X(2) VALUE "--".
            01 WS-SGN PIC S9(3) VALUE -123.
            """, """
            MOVE WS-SGN TO WS-RTHRU.
            DISPLAY "THRU=[" WS-RA WS-RB "]".
            MOVE WS-SGN TO WS-GRCV.
            DISPLAY "TWIN=[" WS-GA WS-GB "]".
            """);
        Assert.Equal("THRU=[12L  ]\nTWIN=[12L  ]", Run(src));
    }

    /// <summary>⛔ "THERE IS NO CONVERSION OF DATA FROM ONE FORM OF INTERNAL REPRESENTATION TO ANOTHER", asked
    /// in BOTH directions of the same clause. A PACKED-DECIMAL elementary item sent INTO a group must deposit
    /// the same bytes it contributes when it is sent AS PART OF a group — §13.18.60.4 leaves that representation
    /// to the implementor (docs/CONFORMANCE.md items 205–215), and whatever it is, the two directions must
    /// agree. They did not: the elementary-sender arm rendered the item's zoned DISPLAY digits, which is the
    /// conversion the clause forbids and also occupies four character positions where the representation
    /// occupies three. The BINARY and COMP-5 siblings take the same arm and are asserted with it.</summary>
    [Theory]
    [InlineData("GR4CNV1", "COMP-3")]
    [InlineData("GR4CNV2", "COMP")]
    [InlineData("GR4CNV3", "COMP-5")]
    public void Gr4_ElementarySenderIntoAGroup_DepositsItsRepresentation_NotItsDisplayImage(string pid, string usage)
    {
        string src = Prog(pid, $"""
            01 WS-PKG.
               05 WS-P PIC 9(4) {usage} VALUE 1234.
            01 WS-IMG1 PIC X(6).
            01 WS-IMG2.
               05 WS-IMG2A PIC X(6).
            """, """
            MOVE WS-PKG TO WS-IMG1.
            MOVE WS-P TO WS-IMG2.
            IF WS-IMG1 = WS-IMG2A
                DISPLAY "AGREES"
            ELSE
                DISPLAY "DIFFERS"
            END-IF.
            """);
        Assert.Equal("AGREES", Run(src));
    }
}
