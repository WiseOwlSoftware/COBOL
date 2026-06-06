// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Data-model migration S3 (the first typed character flip, <c>docs/RECORD_STRUCT_STORAGE_DESIGN.md</c>): a
/// standalone elementary alphanumeric WORKING-STORAGE item, with no byte-observation triggers, is stored as a
/// native .NET <see cref="string"/> field instead of a byte window. These tests run with
/// <c>EnableTypedFields</c> ON (the rest of the corpus runs with it OFF → byte-identical), so they exercise the
/// typed cells: COBOL-correct VALUE init, MOVE-literal store (CobolString.Store), and DISPLAY of the typed field.
/// </summary>
public sealed class TypedFieldFlipTests : EndToEndTestBase
{
    [Fact]
    public void StandaloneAlphanumeric_FlipsToTypedString_ValueInitMoveAndDisplay()
    {
        var (ok, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDX.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(5) VALUE "AB".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                MOVE "CD" TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """, enableTypedFields: true);

        Assert.True(ok, stderr);
        // Typed-field path: VALUE "AB" and MOVE "CD" store into a .NET string field; DISPLAY matches the byte
        // path exactly — alphanumeric DISPLAY trims trailing spaces (PicRuntime.GetDisplayString), so "AB   "→"AB".
        // This output is byte-identical to the flag-OFF byte path (next test), which is the migration's invariant.
        Assert.Equal("AB\nCD", stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TypedFieldFlip_Off_ByDefault_StillByteIdentical()
    {
        // Same program WITHOUT the flag: the byte path produces the identical observable result.
        var (ok, stdout, stderr) = CompileAndRun("""
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDX2.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(5) VALUE "AB".
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-X.
                MOVE "CD" TO WS-X.
                DISPLAY WS-X.
                STOP RUN.
            """);

        Assert.True(ok, stderr);
        Assert.Equal("AB\nCD", stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TypedToTyped_FieldMove_TruncatesToReceiverWidth_ByteIdentical()
    {
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDMV.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(5) VALUE "HELLO".
            01 WS-B PIC X(3).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-A TO WS-B.
                DISPLAY WS-B.
                STOP RUN.
            """;

        // Typed path (both fields flipped): MOVE WS-A TO WS-B re-stores "HELLO" at width 3 → "HEL".
        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        Assert.Equal("HEL", typed.stdout.Replace("\r\n", "\n"));

        // Byte path (flag off): identical observable result — the migration invariant.
        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }
}
