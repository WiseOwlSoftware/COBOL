// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using WiseOwl.Cobol.Tests.Shared;
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ THE IMPLICIT FILLER OF A LEAF-CARRIED STRONGLY-TYPED GROUP TRAVELS WITH IT (kb/Work PB3598). A strongly-typed group with a
/// POINTER leaf has no character image, so a group MOVE, a CALL and a RETURNING delivery carry it as a vector of its members
/// (<c>OoClassTable.LeafCarried</c>); ISO §8.5.1.6.3 makes the bits that advance alignment "within a strongly-typed group item"
/// "implicitly described as a filler elementary bit data item", so they are data of the group, and the vector, built by the
/// record struct (<c>RecordStructEmitter.EmitLeafMethods</c>) and by the storage-area twin (<c>PlaceRenderer.CellLeaves</c>)
/// from the one enumeration <c>BitLayout.FillersOf</c> / <c>WindowFillersOf</c>, carries them after the members.
///
/// <para>The witness is a unit test, not a conformance case: the only portable reading of the filler of such a group is DISPLAY
/// of the group, whose pointer positions are implementor-defined, and a comparison of two groups (§8.8.4.2.12) is not required to
/// look at the filler. The expectation is the round trip itself: the sender's display equals the receiver's after a MOVE and after
/// a CALL that copies the group to a callee's own group and back through a by-reference formal (the storage-area lane).</para>
/// </summary>
public sealed class LeafVectorFillerTests
{
    /// <summary>Strongly-typed groups are a COBOL-2014 introduction.</summary>
    private const int Edition = 2014;

    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB3598LV.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 SG TYPEDEF STRONG GLOBAL.
                  05 SG-B PIC 1 USAGE BIT.
                  05 SG-X PIC X.
                  05 SG-T PIC 1(3) USAGE BIT ALIGNED OCCURS 3.
                  05 SG-P USAGE POINTER.
               01 V1 TYPE SG.
               01 V2 TYPE SG.
               01 V3 TYPE SG.
               01 V4 TYPE SG.
               PROCEDURE DIVISION.
               MAIN-PARA.
                   INITIALIZE V1 WITH FILLER REPLACING BOOLEAN DATA BY B"1"
                   INITIALIZE V3 REPLACING BOOLEAN DATA BY B"1"
                   DISPLAY "V1 [" V1 "]"
                   DISPLAY "V3 [" V3 "]"
                   MOVE V1 TO V2
                   DISPLAY "V2 [" V2 "]"
                   CALL "PB3598SUB" AS NESTED USING V1 V4
                   DISPLAY "V4 [" V4 "]"
                   STOP RUN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB3598SUB.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 W TYPE SG.
               LINKAGE SECTION.
               01 L1 TYPE SG.
               01 L2 TYPE SG.
               PROCEDURE DIVISION USING L1 L2.
                   MOVE L1 TO W
                   DISPLAY "W [" W "]"
                   MOVE W TO L2
                   GOBACK.
               END PROGRAM PB3598SUB.
               END PROGRAM PB3598LV.
        """;

    /// <summary>V1 holds the boolean sender 1 in every boolean item including the implicit filler (its first byte is the bit
    /// 1 and the seven-position filler 1000000: 0xC0), V3 holds it in the named items only (0x80 there), so a display that
    /// ignores the filler cannot tell them apart; the receivers of a MOVE and of a CALL's copy in and out must read as V1.</summary>
    [Fact]
    public void StrongGroup_MoveAndCall_CarryTheImplicitFiller()
    {
        var (ok, output, detail) = new WiseOwlCobolCompiler(Edition).CompileAndRun(Source);
        Assert.True(ok, detail);
        string Line(string tag) => Array.Find(output.Replace("\r\n", "\n").Split('\n'), l => l.StartsWith(tag + " ["))
            ?? throw new Xunit.Sdk.XunitException($"no '{tag}' line in: {output}");
        string v1 = Line("V1");
        Assert.StartsWith("V1 [À", v1);                    // 1 + filler 1000000 = 0xC0: the filler is set at all
        Assert.StartsWith("V3 [\u0080", Line("V3"));            // the control: named items only, filler zero
        Assert.Equal(v1[2..], Line("V2")[2..]);                 // a MOVE carries it
        Assert.Equal(v1[2..], Line("W")[1..]);                  // the callee's MOVE of its by-reference formal carries it in
        Assert.Equal(v1[2..], Line("V4")[2..]);                 // and the copy back out of the formal carries it out
    }
}
