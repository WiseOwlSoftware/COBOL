// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol;
using Xunit;
using WiseOwl.Cobol.Frontend.Preprocessor;

namespace WiseOwl.Cobol.Tests.Unit;

/// <summary>
/// Smoke tests for the greenfield WiseOwl COBOL pipeline via <see cref="CompilerDriver"/>. These anchor the current
/// G2/G3 capability (typed WS fields, MOVE, DISPLAY, arithmetic) and give the G2 bound-tree rebuild a regression
/// net. (The full 364-program differential harness is <c>NistDifferentialTests</c>; see DESIGN §2.)
/// </summary>
public sealed class CompilerDriverTests : WiseOwlCobolTestBase
{
    [Fact]
    public void Hello_DisplaysLiteral()
    {
        var (ok, stdout, detail) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. HELLO.
            PROCEDURE DIVISION.
            MAIN.
                DISPLAY "HELLO FROM WISEOWL".
                STOP RUN.
            """);
        Assert.True(ok, detail);
        Assert.Equal("HELLO FROM WISEOWL", stdout);
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
            Path.Combine(TempDir, "does-not-exist.cob"), SourceFormat: InitialReferenceFormat.Auto));
        Assert.Equal(CompilerDriver.Outcome.SourceNotFound, result.Status);
        Assert.False(result.Success);
    }

    /// <summary>kb/Work PB4502: an output path that names no file in a directory (a volume root on every host; the NUL device,
    /// which `-o /dev/null` from a POSIX shell becomes on Windows) is refused as OutputNotWritable before the bind, where its
    /// null directory once threw ArgumentNullException out of the external repository's search space.</summary>
    [Fact]
    public void CompilerDriver_RefusesAnOutputWithNoDirectory()
    {
        string source = Path.Combine(TempDir, "pb4502.cob");
        File.WriteAllText(source, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB4502.
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       STOP RUN.
            """);
        var outputs = new List<string> { Path.GetPathRoot(Path.GetFullPath(TempDir))! };
        if (OperatingSystem.IsWindows()) outputs.Add("NUL");
        foreach (string output in outputs)
        {
            var result = CompilerDriver.Compile(new CompilerDriver.Options(source, output, SourceFormat: InitialReferenceFormat.Auto));
            Assert.Equal(CompilerDriver.Outcome.OutputNotWritable, result.Status);
            Assert.Contains("names no file in a directory", Assert.Single(result.Errors));
            Assert.Empty(result.OutputFiles);
        }
    }

    /// <summary>kb/Work PB4502 review R1-1: an output the file system refuses AFTER the path checks pass is OutputNotWritable
    /// with the host's message, not an unhandled exception. A directory standing where the `.g.cs` is written makes the
    /// write fail the same way on every host (`-o /dev/null` on Linux fails at that write for the same reason).</summary>
    [Fact]
    public void CompilerDriver_ReportsAnOutputTheFileSystemRefuses()
    {
        string source = Path.Combine(TempDir, "pb4502w.cob");
        File.WriteAllText(source, """
                   IDENTIFICATION DIVISION.
                   PROGRAM-ID. PB4502W.
                   PROCEDURE DIVISION.
                   MAIN-PARA.
                       STOP RUN.
            """);
        string output = Path.Combine(TempDir, "pb4502w-out", "prog.dll");
        Directory.CreateDirectory(Path.ChangeExtension(output, ".g.cs"));
        var result = CompilerDriver.Compile(new CompilerDriver.Options(source, output, SourceFormat: InitialReferenceFormat.Auto));
        Assert.Equal(CompilerDriver.Outcome.OutputNotWritable, result.Status);
        Assert.StartsWith($"cannot write the output of '{output}': ", Assert.Single(result.Errors));
        Assert.False(File.Exists(output));
    }

    [Fact]
    // Owner decision (DEVLOG 519 / VERSION_TEST_MATRIX_DESIGN §10 #2): an unspecified target edition defaults to the
    // LATEST standard (2023), not 85. Edition-specific callers (the NIST harness, the differential harness, per-edition
    // conformance) pass DialectLevel explicitly, so this default flip does not affect them.
    public void CompilerDriver_DefaultsToLatestEdition()
        => Assert.Equal(2023, new CompilerDriver.Options("x.cob", SourceFormat: InitialReferenceFormat.Auto).DialectLevel);
}
