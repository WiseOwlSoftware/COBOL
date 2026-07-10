// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// EXACT-COUNT witnesses for the Step-14g.3 gates — the OO class/interface DEFINITION introductions (COBOL-2002,
/// ISO §11.2/§11.3 / §11.5/§11.6) and the OCCURS DYNAMIC clause (COBOL-2014, ISO §13.18.38 Format 4) — relocated from
/// the bind-time <c>OoClassTable.Build</c> / <c>OdoBindOccursSpec</c> Checks to the post-bind
/// <c>VersionConformancePass</c> parse-arm. All three gate on RECOGNITION (one parse node = one fire): a bound-arm home
/// would UNDER-count the class/interface gates (OoClassTable drops a duplicate/colliding definition BEFORE it enters
/// the built table, yet the former gate fired for every parse node) and OVER-count OCCURS DYNAMIC (a TYPEDEF template's
/// dynamic table is cloned into each <c>TYPE</c> reference, but the former gate fired once per source clause). The
/// version matrix + the contains-based conformance suite verify PRESENCE; these pin the FIRING COUNT.
/// </summary>
public sealed class OoOccursDynEditionTests
{
    private static int Count0900(string source, int edition, string whereFragment)
    {
        var (_, errors, _) = EditionHarness.CompileFull(source, edition);
        return errors.Count(e => e.Contains("COBOLNET0900", StringComparison.OrdinalIgnoreCase)
            && e.Contains(whereFragment, StringComparison.OrdinalIgnoreCase));
    }

    private static string Prog(string wsLines) => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. ODE.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        {wsLines}
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        """;

    private const string DriverPlusClass = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DRV.
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        END PROGRAM DRV.

        IDENTIFICATION DIVISION.
        CLASS-ID. MYCLS.
        IDENTIFICATION DIVISION.
        OBJECT.
        PROCEDURE DIVISION.
        END OBJECT.
        END CLASS MYCLS.
        """;

    private const string DriverPlusInterface = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. DRV.
        PROCEDURE DIVISION.
        MAIN.
            STOP RUN.
        END PROGRAM DRV.

        IDENTIFICATION DIVISION.
        INTERFACE-ID. MYIF.
        END INTERFACE MYIF.
        """;

    /// <summary>OCCURS DYNAMIC below its 2014 introduction produces EXACTLY ONE COBOLNET0900; none at 2014.</summary>
    [Fact]
    public void OccursDynamic_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(Prog("""
            01 WS-TBL.
               05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 1 TO 5.
            """), 85, "the OCCURS DYNAMIC clause"));

    [Fact]
    public void OccursDynamic_At2014_NoGate()
        => Assert.Equal(0, Count0900(Prog("""
            01 WS-TBL.
               05 WS-E PIC 9(3) OCCURS DYNAMIC FROM 1 TO 5.
            """), 2014, "the OCCURS DYNAMIC clause"));

    /// <summary>A class definition below 2002 produces EXACTLY ONE COBOLNET0900 naming the class; none at 2002.</summary>
    [Fact]
    public void ClassDefinition_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(DriverPlusClass, 85, "class definition 'MYCLS'"));

    [Fact]
    public void ClassDefinition_At2002_NoGate()
        => Assert.Equal(0, Count0900(DriverPlusClass, 2002, "class definition 'MYCLS'"));

    /// <summary>An interface definition below 2002 produces EXACTLY ONE COBOLNET0900 naming the interface.</summary>
    [Fact]
    public void InterfaceDefinition_At85_ExactlyOne0900()
        => Assert.Equal(1, Count0900(DriverPlusInterface, 85, "interface definition 'MYIF'"));

    /// <summary>The load-bearing over-count witness: OCCURS DYNAMIC inside a TYPEDEF referenced TWICE gates the 2014
    /// intro EXACTLY ONCE — on the template's single <c>occursClause</c> parse node. The two <c>TYPE</c> clones are
    /// DataItem objects (not parse nodes), so a per-DataItem bound-arm walk would have counted three.</summary>
    [Fact]
    public void OccursDynamicInTypedef_ReferencedTwice_GatesOnce()
        => Assert.Equal(1, Count0900(Prog("""
            01 TDYN TYPEDEF.
               05 E PIC 9(3) OCCURS DYNAMIC FROM 1 TO 5.
            01 A TYPE TDYN.
            01 B TYPE TDYN.
            """), 85, "the OCCURS DYNAMIC clause"));
}
