// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolNet.Tests.Conformance;

/// <summary>
/// kb/Work PB805 — <c>--permissive</c> must accept at least what the strict compiler accepts. §13.16.3 4) ("The
/// remaining clauses may be written in any order") makes <c>01 G VALUE N"AB" GROUP-USAGE NATIONAL.</c> a legal
/// COBOL-2002 national group; §8.9 reserves GROUP-USAGE from 2002, so the only reading is the §13.18.29 clause.
/// The migration mode restores GROUP-USAGE as a user word (it was free at 85), and before the derived list-end
/// predicate (<c>keywordContinuesHere</c>) the VALUE operand list swallowed GROUP-USAGE NATIONAL and drew
/// COBOLNET1639 ×2 + COBOLNET1585. Expected output is read off the source: the group's VALUE N"AB" initializes
/// its one national element A.
/// </summary>
public sealed class PermissiveRestoredWordListTests
{
    private const string Source = """
               IDENTIFICATION DIVISION.
               PROGRAM-ID. PB805-GU-{0}.
               DATA DIVISION.
               WORKING-STORAGE SECTION.
               01 G VALUE N"AB" GROUP-USAGE NATIONAL.
                  05 A PIC N(2).
               PROCEDURE DIVISION.
                   DISPLAY "A=" A.
                   STOP RUN.
        """;

    [Theory]
    [InlineData(2002, false)]
    [InlineData(2002, true)]
    [InlineData(2023, false)]
    [InlineData(2023, true)]
    public void GroupUsageAfterValue_IsTheClause_OnBothSeverityAxes(int edition, bool permissive)
    {
        var (ok, stdout, detail) = EditionHarness.CompileAndRun(
            string.Format(Source, $"{edition}{(permissive ? "P" : "S")}"), edition, permissive);
        Assert.True(ok, detail);
        Assert.Equal("A=AB", stdout.Trim());
    }
}
