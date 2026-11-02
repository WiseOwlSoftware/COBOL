// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace WiseOwl.Cobol.Tests.Conformance;

/// <summary>
/// ⛔ A CONDITION-ONLY POSITION WHOSE WORD A NEARER DATA ITEM TAKES NAMES THAT DATA ITEM (kb/Work PB4219 review R1-1).
/// ISO §8.4.6.2.1 3) a): "If the name is declared in source element B, the item in source element B is the referenced
/// item", so a contained program's own data item RELF hides its container's GLOBAL condition-name RELF, and
/// <c>SymbolTable.NearestDeclaringConditions</c> leaves the condition lookup no survivor. The positions that admit only a
/// condition-name (SET Format 4's condition-name-1, SEARCH ALL's bare WHEN, §14.9.37.2 Format 2) then asked the RAW
/// condition table and told the user the word "is a condition-name (a level-88 entry), which identifies no data item" —
/// the inverse of both facts. <c>ConditionBinder.ReportMisqualifiedCondition</c> now asks
/// <c>DataBinder.ConditionNameHiddenByNearerDataItem</c> and says the reference names a data item; the SEARCH ALL
/// relation screen's "is a condition-name" arm asks the same predicate and leaves the operand to SR8. The codes are pinned
/// by the <c>pb4219_*_names_own_data_item_hiding_container_global_condition_name</c> goldens; this pins the wording, which
/// the code alone does not distinguish.
/// </summary>
public sealed class HiddenConditionNameTests
{
    private const string Container =
        "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. HCNH.\n"
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01 T GLOBAL.\n"
        + "          05 E OCCURS 3 ASCENDING KEY IS K INDEXED BY IX.\n"
        + "             10 K PIC 9.\n"
        + "                88 RELF VALUE 1.\n"
        + "       PROCEDURE DIVISION.\n           CALL \"HCNJ\".\n           STOP RUN.\n";

    private static string Program(string statement) =>
        Container
        + "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. HCNJ.\n"
        + "       DATA DIVISION.\n       WORKING-STORAGE SECTION.\n"
        + "       01 RELF PIC 9 VALUE 3.\n"
        + $"       PROCEDURE DIVISION.\n{statement}\n           GOBACK.\n"
        + "       END PROGRAM HCNJ.\n       END PROGRAM HCNH.\n";

    [Theory]
    [InlineData("           SET RELF TO TRUE.", "'RELF'")]
    [InlineData("           SET RELF (1) TO FALSE.", "'RELF (1)'")]
    [InlineData("           SEARCH ALL E WHEN RELF (IX) DISPLAY \"HIT\" END-SEARCH.", "'RELF (IX)'")]
    public void AConditionOnlyPosition_SaysTheReferenceNamesTheNearerDataItem(string statement, string written)
    {
        var (ok, errors) = EditionHarness.Compile(Program(statement), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertHasDiagnostic(errors, $"{written} names a data item, not a condition-name");
        EditionHarness.AssertHasDiagnostic(errors, "§8.4.6.2.1 3) a)");
        EditionHarness.AssertNoDiagnostic(errors, "is a condition-name (a level-88 entry)");
        EditionHarness.AssertNoDiagnostic(errors, "is not a condition-name: no level-88 entry");
    }

    /// <summary>The SEARCH ALL relation's receiving operand is the data item, so SR8 is its verdict, never Format 2's
    /// "is a condition-name" arm.</summary>
    [Fact]
    public void ASearchAllRelation_ReadsTheNearerDataItemAsItsOperand()
    {
        var (ok, errors) = EditionHarness.Compile(
            Program("           SEARCH ALL E WHEN RELF (IX) = 1 DISPLAY \"HIT\" END-SEARCH."), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL1965");
        EditionHarness.AssertNoDiagnostic(errors, "is a condition-name");
    }

    /// <summary>⛔ A BARE OPERAND ADMITS A DATA ITEM (kb/Work PB4433). IF and EVALUATE take <c>RELF (1)</c> as the contained
    /// program's own scalar data item (§8.4.6.2.1 3) a)), so the verdict is the subscript's §8.4.2.3.3 SR2 (COBOL2096), the
    /// one the same reference draws when no level-88 of the spelling exists; the condition-only wording ("this position
    /// requires a condition-name") is for the positions that admit only a condition-name, and EVALUATE no longer adds a
    /// Table 15 pairing error about a condition the operand never was.</summary>
    [Theory]
    [InlineData("           IF RELF (1) DISPLAY \"A\" END-IF.")]
    [InlineData("           IF NOT RELF (1) DISPLAY \"A\" END-IF.")]
    [InlineData("           PERFORM UNTIL RELF (1) DISPLAY \"A\" END-PERFORM.")]
    [InlineData("           EVALUATE RELF (1) WHEN 3 DISPLAY \"A\" END-EVALUATE.")]
    public void ABareOperand_NamesTheNearerDataItemAndMeetsItsSubscriptRule(string statement)
    {
        var (ok, errors) = EditionHarness.Compile(Program(statement), 2023);
        Assert.False(ok);
        EditionHarness.AssertHasDiagnostic(errors, "COBOL2096");
        EditionHarness.AssertHasDiagnostic(errors, "'RELF (1)': a subscript is written on 'RELF'");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1639");
        EditionHarness.AssertNoDiagnostic(errors, "COBOL1634");
        EditionHarness.AssertNoDiagnostic(errors, "names a data item, not a condition-name");
    }

    /// <summary>The control: with no data item of the spelling in the contained program, the container's GLOBAL
    /// condition-name is the referenced item (§8.4.6.2.1 3) b) 1.) and SET binds it.</summary>
    [Fact]
    public void WithNoNearerDataItem_TheContainersGlobalConditionNameIsNamed()
    {
        string source = Container
            + "       IDENTIFICATION DIVISION.\n       PROGRAM-ID. HCNJ.\n"
            + "       PROCEDURE DIVISION.\n           SET RELF (1) TO TRUE.\n           GOBACK.\n"
            + "       END PROGRAM HCNJ.\n       END PROGRAM HCNH.\n";
        var (ok, errors) = EditionHarness.Compile(source, 2023);
        Assert.True(ok, string.Join("\n", errors));
    }
}
