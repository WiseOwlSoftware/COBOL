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

    [Fact]
    public void AllCharacterGroup_FlipsToRecordStruct_MembersMoveAndDisplay()
    {
        // S3b: an all-character 01 group → a .NET record struct of string members. The members are accessed
        // (MOVE-literal, field MOVE, DISPLAY) as instance.member; byte-identical to the byte path.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDREC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CUSTOMER.
               05 CUST-NAME PIC X(6) VALUE "ACME".
               05 CUST-CITY PIC X(4).
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY CUST-NAME.
                MOVE "OHIO" TO CUST-CITY.
                DISPLAY CUST-CITY.
                MOVE CUST-CITY TO CUST-NAME.
                DISPLAY CUST-NAME.
                STOP RUN.
            """;

        // record-struct path: CUST-NAME init "ACME  "→"ACME"; MOVE "OHIO"→CUST-CITY; MOVE CUST-CITY→CUST-NAME→"OHIO".
        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        Assert.Equal("ACME\nOHIO\nOHIO", typed.stdout.Replace("\r\n", "\n"));

        // byte path (flag off): identical observable result — the migration invariant.
        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TypedToByte_AndByteToTyped_FieldMove_ByteIdentical()
    {
        // WS-TYPED flips to a string field; WS-BYTE is byte-backed (REDEFINES class). MOVE across the boundary
        // materializes via CobolString (Latin-1 round-trips losslessly), so it is byte-identical.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDBND.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-TYPED PIC X(5) VALUE "HELLO".
            01 WS-BYTE  PIC X(5).
            01 WS-R REDEFINES WS-BYTE PIC X(5).
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-TYPED TO WS-BYTE.
                DISPLAY WS-BYTE.
                MOVE "WORLD" TO WS-BYTE.
                MOVE WS-BYTE TO WS-TYPED.
                DISPLAY WS-TYPED.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        Assert.Equal("HELLO\nWORLD", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TypedField_Comparisons_ByteIdentical()
    {
        // IF on typed string fields: vs literal, vs another typed field, and the inequality branch — each
        // materialized to a byte window and run through the existing byte compare, so byte-identical.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDCMP.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC X(4) VALUE "ACME".
            01 WS-B PIC X(4) VALUE "ACME".
            01 WS-C PIC X(4) VALUE "ZZZZ".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A = "ACME" THEN DISPLAY "EQLIT" END-IF.
                IF WS-A = WS-B THEN DISPLAY "EQFLD" END-IF.
                IF WS-A NOT = WS-C THEN DISPLAY "NEFLD" END-IF.
                IF WS-A < WS-C THEN DISPLAY "LTFLD" END-IF.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        Assert.Equal("EQLIT\nEQFLD\nNEFLD\nLTFLD", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }
}
