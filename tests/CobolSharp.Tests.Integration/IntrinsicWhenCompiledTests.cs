// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using System;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// FUNCTION WHEN-COMPILED (ISO/IEC 1989:2023) returns the date/time the program was COMPILED,
/// not the execution-time clock. The compiler bakes it as a constant at emit time (the runtime
/// clock was previously returned, which is wrong — DEVLOG 228).
/// </summary>
public class IntrinsicWhenCompiledTests : EndToEndTestBase
{
    [Fact]
    public void WhenCompiled_ReturnsWellFormedCompileTimestamp()
    {
        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. WHENCMP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-WC PIC X(21).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE FUNCTION WHEN-COMPILED TO WS-WC.
                DISPLAY WS-WC.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        var line = stdout.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];

        // 21-char form: yyyyMMddHHmmsscc±hhmm (date+time, centiseconds, ±zone offset).
        Assert.Equal(21, line.Length);
        Assert.Matches(@"^\d{16}[+-]\d{4}$", line);

        // The date component is the compile date — the test compiles now, so it must be today.
        // (Guards against a regression to the runtime clock only weakly, but locks in the format
        // and that a real timestamp is produced; the compile-time-constant property is verified
        // manually in DEVLOG 228 by running the same DLL twice and getting an identical value.)
        Assert.Equal(DateTime.Now.ToString("yyyyMMdd"), line.Substring(0, 8));
    }
}
