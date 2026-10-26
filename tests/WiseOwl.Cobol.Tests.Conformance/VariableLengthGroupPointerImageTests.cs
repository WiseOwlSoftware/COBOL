// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// kb/Work PB3358 - the CHARACTERS a strongly-typed variable-length group shows at a class pointer or object-reference
/// leaf when it is DISPLAYed. ISO 1989:2023 §14.9.11.4 GR7 leaves the format to the implementor and DOC-A.1-57 defines
/// it: the members' images in declaration order at their current extents, a pointer leaf contributing the storage image
/// of DOC-A.1-216 (NULL of every category is eight zero positions), an object leaf the D-SLOT placeholder of DOC-A.1-214
/// (eight spaces). The golden <c>14.9.11-DISPLAY/pb3358_vlg_display_pointer_leaf</c> cannot pin a zero position (a
/// tracked text file holds no NUL byte, <c>NulByteDriftTests</c>), so it shows each group at an extent with no pointer
/// occurrence and leaves the pointer positions' own characters to this class, whose expected strings are BUILT from the
/// determination, never written as raw bytes. Each shape here is a member kind the composer classifies separately
/// (<c>GroupImageCodec.VarParts</c>): a fixed leaf and a fixed table of them, an OCCURS DEPENDING table of them, and a
/// dynamic-capacity table of them, whose element lambda (<c>GroupImageCodec.DynElementImage</c>) failed the backend
/// compile until it learned the slot-carried element.
/// </summary>
public sealed class VariableLengthGroupPointerImageTests
{
    /// <summary>The NULL image of DOC-A.1-216, <paramref name="n"/> leaves of it.</summary>
    private static string Null(int n) => new((char)0, 8 * n);

    private static string Run(string source)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(source, 2014);
        Assert.True(ok, detail);
        return CutRunner.Normalize(stdout);
    }

    /// <summary>A fixed pointer leaf and a fixed table of program-pointers (§13.18.60.3 SR14 admits both beneath a
    /// STRONG type), at their initial NULL (§13.18.63.4 GR4: class pointer items "are initialized to null"), beside a
    /// dynamic-length item: "hello" + 3 NULL images + "05".</summary>
    [Fact]
    public void FixedPointerLeaves_ShowTheNullImage()
    {
        string stdout = Run("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB3358T1.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 TF IS TYPEDEF STRONG.
                      05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
                      05 TP USAGE POINTER.
                      05 TQ USAGE PROGRAM-POINTER OCCURS 2.
                      05 TN PIC 9(2).
                   01 WS-F TYPE TF.
                   PROCEDURE DIVISION.
                       MOVE "hello" TO TA OF WS-F
                       MOVE 5 TO TN OF WS-F
                       DISPLAY "[" WS-F "]"
                       STOP RUN.
            """);
        Assert.Equal("[hello" + Null(3) + "05]", stdout);
    }

    /// <summary>An OCCURS DEPENDING table of pointers, its count inside the group: DISPLAY sends the group, so the table
    /// is used to the count's value (§13.18.38.4 GR8 b) - two NULL images after "hello", "05" and the count "2".</summary>
    [Fact]
    public void OccursDependingPointerTable_ShowsTheCountsNullImages()
    {
        string stdout = Run("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB3358T2.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 TG IS TYPEDEF STRONG.
                      05 TA PIC X DYNAMIC LENGTH LIMIT IS 10.
                      05 TN PIC 9(2).
                      05 TC PIC 9.
                      05 TP USAGE POINTER OCCURS 0 TO 3 DEPENDING ON TC.
                   01 WS-G TYPE TG.
                   PROCEDURE DIVISION.
                       MOVE "hello" TO TA OF WS-G
                       MOVE 5 TO TN OF WS-G
                       MOVE 2 TO TC OF WS-G
                       SET TP OF WS-G (1) TO NULL
                       SET TP OF WS-G (2) TO NULL
                       DISPLAY "[" WS-G "]"
                       STOP RUN.
            """);
        Assert.Equal("[hello052" + Null(2) + "]", stdout);
    }

    /// <summary>A dynamic-capacity table of program-pointers, then of object references: each occurrence is its 8
    /// positions, the pointer's NULL image or the object's eight-space placeholder. SET of occurrence 2 creates it and
    /// raises the capacity to 2 (§8.5.1.9.3); occurrence 1 is SET too, because an intermediate occurrence is otherwise
    /// undefined (§8.5.1.9.5).</summary>
    [Fact]
    public void DynamicCapacityLeafTables_ShowEachOccurrencesEightPositions()
    {
        string stdout = Run("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB3358T3.
                   ENVIRONMENT DIVISION.
                   CONFIGURATION SECTION.
                   REPOSITORY.
                       CLASS PB3358T3C.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 TH IS TYPEDEF STRONG.
                      05 HA PIC X DYNAMIC LENGTH LIMIT IS 10.
                      05 HQ USAGE PROGRAM-POINTER OCCURS DYNAMIC CAPACITY IN HCAP.
                      05 HB PIC X(2).
                   01 WS-H TYPE TH.
                   01 TR IS TYPEDEF STRONG.
                      05 RA PIC X DYNAMIC LENGTH LIMIT IS 10.
                      05 RR USAGE OBJECT REFERENCE PB3358T3C OCCURS DYNAMIC CAPACITY IN RCAP.
                      05 RB PIC X(2).
                   01 WS-R TYPE TR.
                   PROCEDURE DIVISION.
                       MOVE "ab" TO HB OF WS-H
                       SET HQ OF WS-H (1) TO NULL
                       SET HQ OF WS-H (2) TO NULL
                       DISPLAY "[" WS-H "]"
                       MOVE "cd" TO RB OF WS-R
                       SET RR OF WS-R (1) TO NULL
                       SET RR OF WS-R (2) TO NULL
                       DISPLAY "[" WS-R "]"
                       STOP RUN.
                   END PROGRAM PB3358T3.
                   IDENTIFICATION DIVISION.
                   CLASS-ID. PB3358T3C.
                   END CLASS PB3358T3C.
            """);
        Assert.Equal("[" + Null(2) + "ab]" + "\n" + "[" + new string(' ', 16) + "cd]", stdout);
    }
}
