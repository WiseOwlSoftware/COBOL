// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolSharp.Compiler.Semantics;
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Spec-conformance tests for the Source Text Manipulation module (COPY / REPLACE, ISO §7.2).
/// These target COPY/REPLACE behaviours that the NIST SM suite does not exercise. Each [Fact]
/// asserts DISPLAY output observed from the compiled program. Copybooks are written into
/// <see cref="EndToEndTestBase._tempDir"/> (the source directory, which is on the COPY search
/// path) before compiling, mirroring MiscTests.CopyStatement_ExpandsCopybook.
/// </summary>
public sealed class SourceTextSpecTests : EndToEndTestBase
{
    /// <summary>
    /// Nested COPY: a copied library member may itself contain a COPY statement, which is
    /// expanded recursively (ISO §7.2, Text manipulation; §7.2.3.3 SR 1 forbids a COPY *within*
    /// a COPY statement but permits a copied member to contain its own COPY). OUTER copies INNER;
    /// both fields must be present and usable.
    /// </summary>
    [Fact]
    public void NestedCopy_ExpandsInnerCopybookRecursively()
    {
        File.WriteAllText(Path.Combine(_tempDir, "INNER.cpy"),
            "01 INNER-FIELD PIC X(5) VALUE \"INNER\".\n");
        File.WriteAllText(Path.Combine(_tempDir, "OUTER.cpy"),
            "01 OUTER-FIELD PIC X(5) VALUE \"OUTER\".\nCOPY INNER.\n");

        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. NESTCPY.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            COPY OUTER.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY OUTER-FIELD.
                DISPLAY INNER-FIELD.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("OUTER\nINNER", stdout.Replace("\r\n", "\n"));
    }

    /// <summary>
    /// COPY ... SUPPRESS PRINTING (ISO §7.2.3 Format; GR 4). SUPPRESS is an implementor-defined
    /// listing-only directive: the incorporated text is not listed, but program semantics are
    /// unchanged. Verify the phrase is accepted and the copied item is still present and usable.
    /// </summary>
    [Fact]
    public void CopySuppressPrinting_IsBenignAndStillCopies()
    {
        File.WriteAllText(Path.Combine(_tempDir, "SUPFLD.cpy"),
            "01 SUP-MSG PIC X(11) VALUE \"SUPPRESSED!\".\n");

        var (success, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. SUPCPY.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            COPY SUPFLD SUPPRESS PRINTING.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY SUP-MSG.
                STOP RUN.
            """);

        Assert.True(success, $"Failed: {stderr}");
        Assert.Equal("SUPPRESSED!", stdout);
    }
}

