// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// The no-emit CheckOnly compile path (CompilerDriver.Options.CheckOnly — the CLI `check-batch` / INV-1
/// continuity sweep fast path). Every edition-gating diagnostic is produced in parse + edition-validate +
/// bind/emit, BEFORE the Roslyn backend, so a check-only compile is VERDICT-EQUIVALENT to a full one for the
/// "does this compile at edition X" question while skipping the backend (the dominant cost) and writing nothing.
/// CheckOnly is a strictly LENIENT PREFIX of a full compile: it returns Success once bind succeeds, so it can
/// never FAIL where a full compile succeeds (it only differs by passing a program a full compile would reject at
/// the Roslyn stage — not an edition-continuity concern).
/// </summary>
public sealed class CheckOnlyCompileTests
{
    private static string WriteTemp(string dir, string name, string source)
    {
        string path = Path.Combine(dir, name + ".cob");
        File.WriteAllText(path, source);
        return path;
    }

    private const string GoodProgram = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CHKOK.
        DATA DIVISION.
        WORKING-STORAGE SECTION.
        01 WS-R PIC 9(4).
        PROCEDURE DIVISION.
        MAIN.
            COMPUTE WS-R = 2 + 2.
            DISPLAY WS-R.
            STOP RUN.
        """;

    // DELETE FILE (Format 2) is a 2023 construct — rejected below 2023 (COBOLNET0900).
    private const string EditionGated2023 = """
        IDENTIFICATION DIVISION.
        PROGRAM-ID. CHK23.
        ENVIRONMENT DIVISION.
        INPUT-OUTPUT SECTION.
        FILE-CONTROL.
            SELECT F ASSIGN "F" ORGANIZATION LINE SEQUENTIAL.
        DATA DIVISION.
        FILE SECTION.
        FD F.
        01 F-REC PIC X(10).
        PROCEDURE DIVISION.
        MAIN.
            DELETE FILE F.
            STOP RUN.
        """;

    [Fact]
    public void CheckOnly_GoodProgram_SucceedsAndEmitsNothing()
    {
        string dir = Directory.CreateTempSubdirectory("chkonly").FullName;
        try
        {
            string src = WriteTemp(dir, "CHKOK", GoodProgram);
            var r = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: 2023, CheckOnly: true));
            Assert.True(r.Success, string.Join("\n", r.Errors));
            // No runnable assembly and no generated C# were written (the backend + file writes are skipped).
            Assert.Empty(Directory.GetFiles(dir, "*.dll"));
            Assert.Empty(Directory.GetFiles(dir, "*.g.cs"));
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Theory]
    [InlineData(2023, true)]    // DELETE FILE is valid at 2023
    [InlineData(85, false)]     // and rejected below it — CheckOnly must report the SAME verdict as a full compile
    [InlineData(2014, false)]
    public void CheckOnly_EditionGating_MatchesFullCompileVerdict(int edition, bool expectSuccess)
    {
        string dir = Directory.CreateTempSubdirectory("chkedn").FullName;
        try
        {
            string src = WriteTemp(dir, "CHK23", EditionGated2023);
            var check = CompilerDriver.Compile(new CompilerDriver.Options(src, DialectLevel: edition, CheckOnly: true));
            var full = CompilerDriver.Compile(new CompilerDriver.Options(
                src, OutputPath: Path.Combine(dir, "full.dll"), DialectLevel: edition));
            Assert.Equal(expectSuccess, check.Success);
            // The edition-gating verdict is identical whether or not the backend runs.
            Assert.Equal(full.Success, check.Success);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
