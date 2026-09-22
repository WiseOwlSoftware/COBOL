// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ⛔ <b>A level-66 THROUGH alias IS A GROUP ITEM for ISO §14.9.25.3 SR9 / §14.9.25.4 GR9</b> (kb/Work PB907).
/// §13.18.45.4 GR2 — "When the THROUGH phrase is specified, data-name-1 defines an alphanumeric group item that
/// includes all elementary items starting with data-name-2 …" — so a MOVE between an alias and a variable-length
/// group is a move between two group items, and §8.5.1.12's compatibility relation decides it. The screen asked
/// the STRUCTURAL <c>IsGroup</c> instead and told the user "the sending operand is not a group item".
/// <para>Every assertion is an EQUIVALENCE with the alias's STRUCTURAL TWIN — a plain group with the alias's
/// layout — which is what GR2 requires, so no case pins a rendering this implementation merely happens to
/// produce. The positive half is the corpus golden <c>2014/pb907_through_alias_vlg_move</c>.</para>
/// </summary>
public sealed class ThroughAliasGroupItemTests
{
    private const string Alias = """
        01 SRC.
           05 S1 PIC X(3) VALUE "ABC".
           05 S2 PIC X(3) VALUE "DEF".
        66 SALIAS RENAMES S1 THROUGH S2.
        """;

    private const string Twin = """
        01 SRC.
           05 SALIAS.
              10 S1 PIC X(3) VALUE "ABC".
              10 S2 PIC X(3) VALUE "DEF".
        """;

    private static string Prog(string pid, string ws, string proc) => $"""
               IDENTIFICATION DIVISION.
               PROGRAM-ID. {pid}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
        {ws}
               01 DST.
                  05 D1 PIC X(1) OCCURS DYNAMIC CAPACITY IN CAPN.
               PROCEDURE DIVISION.
               MAIN.
                   {proc}
                   STOP RUN.
        """;

    /// <summary>The incompatible pair, in BOTH directions: the alias draws exactly the §8.5.1.12.2 reason its
    /// structural twin draws, and never the false kind claim.</summary>
    [Theory]
    [InlineData("MOVE SALIAS TO DST")]
    [InlineData("MOVE DST TO SALIAS")]
    public void IncompatibleAlias_DrawsTheCompatibilityReason_LikeItsStructuralTwin(string stmt)
    {
        var alias = EditionHarness.GetDiagnostics(Prog("PB907TA", Indent(Alias), stmt), 2023);
        var twin = EditionHarness.GetDiagnostics(Prog("PB907TT", Indent(Twin), stmt), 2023);
        const string reason = "the dynamic-capacity table 'D1' of 'DST' occupies relative byte position 0 and "
            + "the other group has no table there";
        Assert.Contains(twin, d => d.Contains(reason));
        Assert.Contains(alias, d => d.Contains(reason));
        Assert.DoesNotContain(alias, d => d.Contains("is not a group item"));
    }

    private static string Indent(string ws) =>
        string.Join("\n", ws.Replace("\r\n", "\n").Split('\n').Select(l => "       " + l));
}
