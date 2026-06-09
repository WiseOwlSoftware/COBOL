// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet;
using Xunit;

namespace CobolNet.Tests.Unit;

/// <summary>
/// Smoke tests for the greenfield COBOL.NET pipeline via <see cref="CompilerDriver"/>. These anchor the current
/// G2/G3 capability (typed WS fields, MOVE, DISPLAY, arithmetic) and give the G2 bound-tree rebuild a regression
/// net. (The full 364-program differential harness against the legacy oracle lands at G5; see COBOLNET_DESIGN §2.)
/// </summary>
public sealed class CompilerDriverTests : CobolNetTestBase
{
    [Fact]
    public void Hello_DisplaysLiteral()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HELLO.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "HELLO FROM COBOLNET".
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Equal("HELLO FROM COBOLNET", stdout);
    }

    [Fact]
    public void MoveAndDisplay_TypedFields()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. MV.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-NAME PIC X(6) VALUE "BOB".
            01 WS-N    PIC 9(4) VALUE 5.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "NAME=" WS-NAME.
                DISPLAY "N=" WS-N.
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Equal("NAME=BOB   \r\nN=0005", stdout);
    }

    [Fact]
    public void CompilerDriver_ReportsSourceNotFound()
    {
        var result = CompilerDriver.Compile(new CompilerDriver.Options(
            Path.Combine(TempDir, "does-not-exist.cob")));
        Assert.Equal(CompilerDriver.Outcome.SourceNotFound, result.Status);
        Assert.False(result.Success);
    }
}
