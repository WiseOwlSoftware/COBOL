// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ ONLY THE DECLARING ELEMENT CLAIMS A CELL FOR ITS STORAGE (ISO §13.18.27.4 GR2: "A statement in a program contained
/// directly or indirectly within a program that describes a global name may reference that name without describing it
/// again"; §14.2.3 GR8: "If the argument is passed by reference, the activated runtime element operates as if the formal
/// parameter occupies the same storage area as the argument"). A contained program's <c>ADDRESS OF</c> head or BY
/// REFERENCE operand that names a container's GLOBAL item is claimed by the CONTAINER, which scans its contained programs
/// (<c>DataBinder.PtrBindBasedAndAddressables</c>); the contained program reaches the cell through its GLOBAL bridge.
/// Once the containers' GLOBAL names became visible before the contained unit's own storage pass (kb/Work PB3766's
/// <c>InheritContainerGlobalData</c>), that pass resolved the head to the inherited item and claimed the cell a second
/// time, so the contained program's C# type declared <c>_scell_R2</c> and <c>_redef_R2</c> beside the bridge's (CS0102;
/// merge group 41183181188, pull request #880). <c>ResolveStorageClaimHead</c> answers only an item of the head's own
/// element. Each arm here is one shape of that rule: unqualified, qualified, BY REFERENCE, two containment levels out,
/// and a contained program's own record that shares the name (which IS its own to claim).
/// </summary>
public sealed class ContainedGlobalStorageClaimTests
{
    private const string AddressOfFromInner = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. CGSC-OUT.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 R1.
                  05 X PIC X(4) VALUE "LOCL".
               01 R2 GLOBAL.
                  05 X PIC X(4) VALUE "GLOB".
               PROCEDURE DIVISION.
                   CALL "CGSC-IN"
                   DISPLAY R2
                   STOP RUN.
               IDENTIFICATION DIVISION.
               PROGRAM-ID. CGSC-IN.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 P USAGE POINTER.
               01 V PIC X(4) BASED.
               PROCEDURE DIVISION.
                   SET P TO ADDRESS OF {0}
                   SET ADDRESS OF V TO P
                   DISPLAY V
                   MOVE "NEW!" TO V
                   GOBACK.
               END PROGRAM CGSC-IN.
               END PROGRAM CGSC-OUT.
        """;

    [Theory]
    [InlineData("X")]          // unqualified: R2's X is the only one visible in CGSC-IN (R1 is the container's local)
    [InlineData("X OF R2")]    // qualified through the one §8.4.2.2 machinery
    [InlineData("R2")]         // the GLOBAL record itself
    public void AddressOfAContainersGlobal_IsTheContainersCell(string head)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(string.Format(AddressOfFromInner, head), 2023);
        Assert.True(ok, detail);
        // The pointer addresses the container's storage: the contained program reads GLOB through it, and its store
        // is what the container then displays.
        Assert.Equal(["GLOB", "NEW!"], Lines(stdout));
    }

    [Fact]
    public void ByReferenceOfAContainersGlobal_SharesTheContainersArea()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSB-OUT.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 R2 GLOBAL.
                      05 X PIC X(4) VALUE "GLOB".
                   PROCEDURE DIVISION.
                       CALL "CGSB-IN"
                       DISPLAY R2
                       STOP RUN.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSB-IN.
                   PROCEDURE DIVISION.
                       CALL "CGSB-SUB" USING X
                       GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSB-SUB.
                   DATA DIVISION.
                   LINKAGE SECTION.
                   01 LK PIC X(4).
                   PROCEDURE DIVISION USING LK.
                       MOVE "SUB!" TO LK
                       DISPLAY R2
                       GOBACK.
                   END PROGRAM CGSB-SUB.
                   END PROGRAM CGSB-IN.
                   END PROGRAM CGSB-OUT.
            """, 2023);
        Assert.True(ok, detail);
        // GR8: the formal occupies the GLOBAL item's storage, so R2 holds SUB! before the return, and after it.
        Assert.Equal(["SUB!", "SUB!"], Lines(stdout));
    }

    [Fact]
    public void AddressOfAGlobalTwoLevelsOut_IsTheDeclaringContainersCell()
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSD-OUT.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 R2 GLOBAL.
                      05 X PIC X(4) VALUE "GLOB".
                   PROCEDURE DIVISION.
                       CALL "CGSD-MID"
                       DISPLAY R2
                       STOP RUN.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSD-MID.
                   PROCEDURE DIVISION.
                       CALL "CGSD-IN"
                       GOBACK.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSD-IN.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 P USAGE POINTER.
                   01 V PIC X(4) BASED.
                   PROCEDURE DIVISION.
                       SET P TO ADDRESS OF X OF R2
                       SET ADDRESS OF V TO P
                       DISPLAY V
                       MOVE "DEEP" TO V
                       GOBACK.
                   END PROGRAM CGSD-IN.
                   END PROGRAM CGSD-MID.
                   END PROGRAM CGSD-OUT.
            """, 2023);
        Assert.True(ok, detail);
        Assert.Equal(["GLOB", "DEEP"], Lines(stdout));
    }

    [Fact]
    public void AContainedProgramsOwnRecord_HidesTheGlobal_AndIsItsOwnToClaim()
    {
        // §8.4.6.2.1 3): CGSS-IN's own R3's X is the nearer declaration, so ADDRESS OF X is R3's, claimed by CGSS-IN;
        // ADDRESS OF R2 still names the container's GLOBAL record, claimed by the container.
        var (ok, stdout, detail) = EditionHarness.CompileAndRun("""
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSS-OUT.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 R2 GLOBAL.
                      05 X PIC X(4) VALUE "GLOB".
                   PROCEDURE DIVISION.
                       CALL "CGSS-IN"
                       DISPLAY R2
                       STOP RUN.
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. CGSS-IN.
                   DATA DIVISION.
                   WORKING-STORAGE SECTION.
                   01 R3.
                      05 X PIC X(4) VALUE "MINE".
                   01 P USAGE POINTER.
                   01 V PIC X(4) BASED.
                   PROCEDURE DIVISION.
                       SET P TO ADDRESS OF X
                       SET ADDRESS OF V TO P
                       DISPLAY V
                       MOVE "CHGD" TO V
                       DISPLAY R3
                       SET P TO ADDRESS OF R2
                       SET ADDRESS OF V TO P
                       MOVE "VIAP" TO V
                       GOBACK.
                   END PROGRAM CGSS-IN.
                   END PROGRAM CGSS-OUT.
            """, 2023);
        Assert.True(ok, detail);
        Assert.Equal(["MINE", "CHGD", "VIAP"], Lines(stdout));
    }

    private static string[] Lines(string stdout) =>
        stdout.Replace("\r\n", "\n").TrimEnd('\n').Split('\n').Select(l => l.TrimEnd()).ToArray();
}
