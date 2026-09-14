// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// ISO §14.9.37.4 — WHERE THE SEARCH STATEMENT LEAVES ITS SEARCH INDEX, per FORMAT.
/// <para>The standard writes the scan twice and the two texts disagree, which is the whole subject of this class
/// (kb/Work PB447). Format 1, GR4: "the search index is incremented by one occurrence number. The process is then
/// repeated using the new index setting <b>unless</b> the new value for the search index corresponds to a table
/// element outside the permissible range of occurrence values" — the new value is FORMED and then judged, so an
/// unsuccessful serial search ends one past the table, and the rule says so in as many words. Format 2, GR9: "At
/// no time is it set to a value that exceeds the value that corresponds to the last element of the table or is
/// less than the value that corresponds to the first element of the table" — unconditional, and binding on the
/// unsuccessful exit as much as on the probes, because GR1 b) 1. makes the AT END phrase part of this statement's
/// execution and a subscript written there reads through the search index.</para>
/// <para>⛔ BOTH ARMS ARE PINNED, and that is the point. ONE advance-then-test loop served both formats until
/// PB447; it was correct for Format 1 and silently parked a five-occurrence SEARCH ALL index at 6, so
/// <c>K(KX)</c> under AT END read storage outside the table. A class that pinned only the Format-2 bound would
/// invite "fixing" Format 1 to match it — GR4's overshoot is REQUIRED, not tolerated.</para>
/// <para>The Format-2 assertions are RANGE assertions, never equality: GR9 ends "the final setting of the search
/// index is undefined" for an unsuccessful search, so which in-range occurrence is left is not a promise this
/// compiler may be held to — while being inside the table is. Format 1's final value IS determined by GR4, so it
/// is pinned exactly.</para>
/// </summary>
public sealed class SearchIndexRangeSpecTests
{
    /// <summary>A program with a five-occurrence keyed table holding 1 3 5 7 9 (ASCENDING, so §14.9.37.4 GR5 a) is
    /// satisfied and GR9 — not GR6 — governs), plus an OCCURS DEPENDING twin whose current count the caller
    /// chooses. <paramref name="body"/> is the PROCEDURE DIVISION text under test.</summary>
    private static string Program(string programId, string body, string odoMin = "1") => $"""
        IDENTIFICATION DIVISION.
        PROGRAM-ID. {programId}.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 N   PIC 99.
        01 C   PIC 9 VALUE 3.
        01 T.
           05 EL OCCURS 5 ASCENDING KEY IS K INDEXED BY KX.
              10 K PIC 9.
        01 U.
           05 EY OCCURS {odoMin} TO 5 TIMES DEPENDING ON C
              ASCENDING KEY IS J INDEXED BY JX.
              10 J PIC 9.
        PROCEDURE DIVISION.
        MAIN-P.
            MOVE 1 TO K (1).
            MOVE 3 TO K (2).
            MOVE 5 TO K (3).
            MOVE 7 TO K (4).
            MOVE 9 TO K (5).
            MOVE 1 TO J (1).
            MOVE 3 TO J (2).
            MOVE 5 TO J (3).
        {body}
            STOP RUN.
        """;

    /// <summary>Compile-and-run at <paramref name="edition"/> and return the program's stdout lines.</summary>
    private static string[] Run(string source, int edition)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(source, edition);
        Assert.True(ok, $"[--std {edition}] {detail}");
        return stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>The integer a <c>DISPLAY "TAG=" N</c> line carries (N is PIC 99, so "IX=04").</summary>
    private static int Tagged(string[] lines, string tag)
    {
        string? line = lines.FirstOrDefault(l => l.StartsWith(tag + "=", StringComparison.Ordinal));
        Assert.True(line is not null, $"no '{tag}=' line in:\n{string.Join("\n", lines)}");
        return int.Parse(line![(tag.Length + 1)..].Trim());
    }

    public static TheoryData<int> AllEditions()
    {
        var d = new TheoryData<int>();
        foreach (int e in EditionHarness.Editions) d.Add(e);
        return d;
    }

    [Theory]   // GR9's range bound on the unsuccessful exit of Format 2. Measured before PB447: KX = 6 of 5.
    [MemberData(nameof(AllEditions))]
    public void SearchAll_Unsuccessful_LeavesTheIndexInsideTheTable(int edition)
    {
        var lines = Run(Program("PB447A", """
                SEARCH ALL EL
                    AT END SET N TO KX
                           DISPLAY "ATEND=" N
                    WHEN K (KX) = 4
                        DISPLAY "FOUND"
                END-SEARCH.
                SET N TO KX.
                DISPLAY "AFTER=" N.
            """), edition);
        Assert.InRange(Tagged(lines, "ATEND"), 1, 5);
        Assert.InRange(Tagged(lines, "AFTER"), 1, 5);
    }

    [Theory]   // The harm the bound exists to prevent: the AT END phrase is part of the SEARCH statement's own
    [MemberData(nameof(AllEditions))]   // execution (GR1 b) 1.), so a subscript written there reads through the
    // search index. With the index parked at 6 this DISPLAY printed 0 from outside the table's storage, and no
    // diagnostic appeared because bound checking is off by default — the silent wrong answer PB447 charges.
    public void SearchAll_Unsuccessful_SubscriptInsideAtEnd_ReadsARealElement(int edition)
    {
        var lines = Run(Program("PB447B", """
                SEARCH ALL EL
                    AT END DISPLAY "READ=" K (KX)
                    WHEN K (KX) = 4
                        DISPLAY "FOUND"
                END-SEARCH.
            """), edition);
        // The five occurrences hold 1 3 5 7 9; anything else came from outside the table.
        Assert.Contains(Tagged(lines, "READ"), new[] { 1, 3, 5, 7, 9 });
    }

    [Theory]   // §13.18.38 GR7 makes an occurs-depending table's CURRENT count the last element, so GR9's bound
    [MemberData(nameof(AllEditions))]   // is 3 here even though storage for 5 exists — the case where "inside the
    // table" and "inside the storage" differ, and the one an off-by-one is likeliest to survive.
    public void SearchAll_UnsuccessfulOnAnOccursDependingTable_IsBoundedByTheCurrentCount(int edition)
    {
        var lines = Run(Program("PB447C", """
                SEARCH ALL EY
                    AT END SET N TO JX
                           DISPLAY "ATEND=" N
                           DISPLAY "READ=" J (JX)
                    WHEN J (JX) = 4
                        DISPLAY "FOUND"
                END-SEARCH.
            """), edition);
        Assert.InRange(Tagged(lines, "ATEND"), 1, 3);
        Assert.Contains(Tagged(lines, "READ"), new[] { 1, 3, 5 });
    }

    [Theory]   // An EMPTY table has no first or last element, so GR9's bound names no occurrence and no probe can
    [MemberData(nameof(AllEditions))]   // be made: the search is unsuccessful at once. The index is left at the 1
    // GR9's ignored-initial-setting rule put there, the only value that is not a claim about a table element.
    public void SearchAll_OnAnEmptyTable_MakesNoProbeAndLeavesTheIndexAtOne(int edition)
    {
        var lines = Run(Program("PB447D", """
                MOVE 0 TO C.
                SEARCH ALL EY
                    AT END SET N TO JX
                           DISPLAY "ATEND=" N
                    WHEN J (JX) = 1
                        DISPLAY "FOUND"
                END-SEARCH.
            """, odoMin: "0"), edition);
        Assert.Equal(1, Tagged(lines, "ATEND"));
    }

    [Theory]   // GR1 a): on a successful search "the index being varied by the search operation remains set at the
    [MemberData(nameof(AllEditions))]   // occurrence number that caused a WHEN condition to be satisfied". Exactly
    // one occurrence holds 5, so GR7's "undefined which one" does not arise and the value is determined.
    public void SearchAll_Successful_LeavesTheIndexAtTheMatchingOccurrence(int edition)
    {
        var lines = Run(Program("PB447E", """
                SEARCH ALL EL
                    AT END DISPLAY "ATEND"
                    WHEN K (KX) = 5
                        SET N TO KX
                        DISPLAY "FOUND=" N
                END-SEARCH.
            """), edition);
        Assert.Equal(3, Tagged(lines, "FOUND"));
    }

    [Theory]   // GR9: "The initial setting of the search index is ignored." A SET past the only match would hide
    [MemberData(nameof(AllEditions))]   // it if the scan honoured the incoming value the way Format 1 must.
    public void SearchAll_IgnoresTheInitialSettingOfTheSearchIndex(int edition)
    {
        var lines = Run(Program("PB447F", """
                SET KX TO 5.
                SEARCH ALL EL
                    AT END DISPLAY "ATEND"
                    WHEN K (KX) = 1
                        SET N TO KX
                        DISPLAY "FOUND=" N
                END-SEARCH.
            """), edition);
        Assert.Equal(1, Tagged(lines, "FOUND"));
    }

    [Theory]   // ⛔ THE OTHER ARM. GR4 requires the overshoot Format 2 forbids: the new index value is formed
    [MemberData(nameof(AllEditions))]   // before the rule judges it, so an unsuccessful serial search over five
    // occurrences ends at 6. Pinned so the PB447 bound cannot be "swept" onto the format that is licensed for it.
    public void Serial_Unsuccessful_LeavesTheIndexOnePastTheTable(int edition)
    {
        var lines = Run(Program("PB447G", """
                SET KX TO 1.
                SEARCH EL
                    AT END SET N TO KX
                           DISPLAY "ATEND=" N
                    WHEN K (KX) = 4
                        DISPLAY "FOUND"
                END-SEARCH.
            """), edition);
        Assert.Equal(6, Tagged(lines, "ATEND"));
    }

    [Theory]   // GR3 b) 2.: an integer identifier-2 "is incremented by the value one at the same time as the
    [MemberData(nameof(AllEditions))]   // search index is incremented" — AT THE SAME TIME, so it takes the final
    // increment too and ends at 15 for a scan that starts it at 10 and runs the index 1 → 6.
    public void Serial_VaryingAnIntegerItem_TakesEveryIncrementIncludingTheLast(int edition)
    {
        var lines = Run(Program("PB447H", """
                MOVE 10 TO N.
                SET KX TO 1.
                SEARCH EL VARYING N
                    AT END DISPLAY "ATEND=" N
                    WHEN K (KX) = 4
                        DISPLAY "FOUND"
                END-SEARCH.
            """), edition);
        Assert.Equal(15, Tagged(lines, "ATEND"));
    }

    [Theory]   // GR4's OTHER unsuccessful leg, the one Format 2 cannot reach: an initial setting outside the
    [MemberData(nameof(AllEditions))]   // permissible range is unsuccessful BEFORE any probe, and the index is
    // not varied at all ("starting from the occurrence number that corresponds to the value of the search index
    // at the beginning of the execution").
    public void Serial_InitialIndexPastTheEnd_IsUnsuccessfulWithoutVaryingTheIndex(int edition)
    {
        var lines = Run(Program("PB447I", """
                SET KX TO 6.
                SEARCH EL
                    AT END SET N TO KX
                           DISPLAY "ATEND=" N
                    WHEN K (KX) = 1
                        DISPLAY "FOUND"
                END-SEARCH.
            """), edition);
        Assert.Equal(6, Tagged(lines, "ATEND"));
    }

    [Fact]   // The D9 shape of the same bound: an OCCURS DYNAMIC table's last element is its CURRENT capacity
             // (§8.5.1.9.1), so GR9 bounds the SEARCH ALL index by that and not by the declared maximum. OCCURS
             // DYNAMIC is a COBOL-2023 construct, so this arm has one edition.
    public void SearchAll_UnsuccessfulOnADynamicTable_IsBoundedByTheCurrentCapacity()
    {
        var lines = Run("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. PB447J.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 N   PIC 99.
            01 D.
               05 DROW OCCURS DYNAMIC CAPACITY IN CAP FROM 1 TO 8
                  ASCENDING KEY IS DKEY INDEXED BY DX.
                  10 DKEY PIC 9.
            PROCEDURE DIVISION.
            MAIN-P.
                SET CAP TO 3.
                MOVE 1 TO DKEY (1).
                MOVE 3 TO DKEY (2).
                MOVE 5 TO DKEY (3).
                SEARCH ALL DROW
                    AT END SET N TO DX
                           DISPLAY "ATEND=" N
                           DISPLAY "READ=" DKEY (DX)
                    WHEN DKEY (DX) = 4
                        DISPLAY "FOUND"
                END-SEARCH.
                STOP RUN.
            """, 2023);
        Assert.InRange(Tagged(lines, "ATEND"), 1, 3);
        Assert.Contains(Tagged(lines, "READ"), new[] { 1, 3, 5 });
    }
}
