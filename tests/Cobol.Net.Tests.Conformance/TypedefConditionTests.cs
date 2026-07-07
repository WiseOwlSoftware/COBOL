// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// level-88 condition-names inside a TYPEDEF (Phase 6, data-model D17, increment 3; ISO §13.18.58.4 GR1): the
/// condition-names are PART of the type, so each <c>TYPE</c> reference gets its own clone of them (testing / SETting
/// its own storage), while the TEMPLATE's condition-names are NOT globally referenceable (GR1). The run-success
/// corpus (<c>typedef_88</c>) byte-verifies the two-record independence + qualified resolution; this locks the
/// compile-time properties: cloned 88s resolve, and the template's names do not leak into the global by-name index.
/// </summary>
public sealed class TypedefConditionTests
{
    /// <summary>A cloned condition-name resolves — both <c>SET cond TO TRUE</c> and the <c>IF cond</c> test — even
    /// UNQUALIFIED with a single clone. This also guards GR1: were the template's <c>OPEN-ST</c> still registered
    /// globally (the pre-inc-3 leak), the unqualified reference would be an ambiguous two-entry match.</summary>
    [Fact]
    public void ClonedConditionResolvesUnqualified_CompileClean()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TCOND1.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 STATE-T TYPEDEF.
               05 ST-CODE PIC X.
                  88 OPEN-ST VALUE "O".
            01 DOOR TYPE STATE-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET OPEN-ST TO TRUE.
                IF OPEN-ST
                    DISPLAY "OK"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.True(ok, $"a cloned + unqualified condition-name must resolve cleanly (no template GR1 leak): "
            + string.Join("; ", diag));
    }

    /// <summary>Two records of the same TYPEDEF get INDEPENDENT condition-name clones — the duplicate names resolve
    /// by qualifier (<c>OPEN-ST OF DOOR</c>) with no ambiguity error (§8.4.2.2 Format 2 over the cloned parents).</summary>
    [Fact]
    public void TwoRecordsQualifiedConditions_CompileClean()
    {
        var (ok, diag) = EditionHarness.Compile("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TCOND2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 STATE-T TYPEDEF.
               05 ST-CODE PIC X.
                  88 OPEN-ST   VALUE "O".
                  88 CLOSED-ST VALUE "C".
            01 DOOR TYPE STATE-T.
            01 GATE TYPE STATE-T.
            PROCEDURE DIVISION.
            MAIN-PARA.
                SET OPEN-ST OF DOOR TO TRUE.
                MOVE "C" TO ST-CODE OF GATE.
                IF OPEN-ST OF DOOR AND CLOSED-ST OF GATE
                    DISPLAY "BOTH"
                END-IF.
                STOP RUN.
            """, 2002);
        Assert.True(ok, $"per-record cloned condition-names must resolve by qualifier: {string.Join("; ", diag)}");
    }
}
