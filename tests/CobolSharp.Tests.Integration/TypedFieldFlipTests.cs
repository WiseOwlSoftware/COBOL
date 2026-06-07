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

    [Fact]
    public void MoveFigurative_SpaceAndZero_ToTypedField_ByteIdentical()
    {
        // MOVE SPACES / ZEROS to a typed field (field clearing). DISPLAY trims, so the cleared field shows via
        // a trailing marker; byte-identical to the byte path.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDFIG.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-X PIC X(4) VALUE "ABCD".
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE ZEROS TO WS-X.
                DISPLAY WS-X.
                MOVE SPACES TO WS-X.
                DISPLAY WS-X "|".
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // ZEROS → "0000"; SPACES → "    " (DISPLAY trims → "" then "|").
        Assert.Equal("0000\n|", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void TypedField_ClassConditions_ByteIdentical()
    {
        // IS NUMERIC / IS ALPHABETIC on typed string fields — the subject is a read-only sender, materialized to
        // a byte window and run through the same byte class check, so byte-identical.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDCLS.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-DIGITS PIC X(3) VALUE "123".
            01 WS-ALPHA  PIC X(3) VALUE "ABC".
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-DIGITS IS NUMERIC THEN DISPLAY "DNUM" END-IF.
                IF WS-ALPHA IS NUMERIC THEN DISPLAY "ANUM" ELSE DISPLAY "ANOTNUM" END-IF.
                IF WS-ALPHA IS ALPHABETIC THEN DISPLAY "AALPHA" END-IF.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        Assert.Equal("DNUM\nANOTNUM\nAALPHA", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void UnsignedIntegerDisplay_FlipsToTypedLong_ValueInitMoveAndDisplay_ByteIdentical()
    {
        // S4: standalone unsigned-integer DISPLAY items with a VALUE flip to typed .NET long fields. VALUE init,
        // MOVE-literal (incl. high-order truncation and fraction truncation), and DISPLAY (zero-padded digit
        // image) are all byte-identical to the byte path.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDNUM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(5) VALUE 42.
            01 WS-B PIC 9(3) VALUE 7.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-A.
                MOVE 1234567 TO WS-A.
                DISPLAY WS-A.
                MOVE 3.9 TO WS-B.
                DISPLAY WS-B.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // VALUE 42 → "00042"; MOVE 1234567 → low 5 digits "34567"; MOVE 3.9 → truncate → "003".
        Assert.Equal("00042\n34567\n003", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NumericToNumeric_FieldMove_ByteIdentical()
    {
        // MOVE between two typed numeric (long) fields: dst = src truncated to dst's digit count. Both widening
        // (3→5: "00042") and high-order truncation (5→3: low 3 digits) are byte-identical to the byte path.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDNMV.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-SMALL PIC 9(3) VALUE 42.
            01 WS-WIDE  PIC 9(5) VALUE 0.
            01 WS-BIG   PIC 9(5) VALUE 12345.
            01 WS-NARROW PIC 9(3) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-SMALL TO WS-WIDE.
                DISPLAY WS-WIDE.
                MOVE WS-BIG TO WS-NARROW.
                DISPLAY WS-NARROW.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // 42 → PIC 9(5) "00042"; 12345 → PIC 9(3) low 3 digits "345".
        Assert.Equal("00042\n345", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NumericField_Comparisons_ByteIdentical()
    {
        // S4 numeric sender-materialize: IF on typed numeric (long) fields — vs literal (=, >, <), vs another
        // typed numeric field, and IS NUMERIC. Each typed operand is encoded back to its digit window
        // (PicRuntime.EncodeNumeric) and run through the existing byte numeric compare/class check, so the value
        // round-trips and the result is byte-identical to the byte path.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDNCM.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(5) VALUE 42.
            01 WS-B PIC 9(5) VALUE 42.
            01 WS-C PIC 9(5) VALUE 99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                IF WS-A = 42 THEN DISPLAY "EQLIT" END-IF.
                IF WS-A > 40 THEN DISPLAY "GTLIT" END-IF.
                IF WS-A < 99 THEN DISPLAY "LTLIT" END-IF.
                IF WS-A = WS-B THEN DISPLAY "EQFLD" END-IF.
                IF WS-A NOT = WS-C THEN DISPLAY "NEFLD" END-IF.
                IF WS-A IS NUMERIC THEN DISPLAY "ISNUM" END-IF.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        Assert.Equal("EQLIT\nGTLIT\nLTLIT\nEQFLD\nNEFLD\nISNUM", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NumericField_Arithmetic_ByteIdentical()
    {
        // S4 numeric arithmetic on typed (long) fields: ADD…TO (read-modify-write receiver), SUBTRACT…FROM,
        // MULTIPLY…GIVING, DIVIDE…GIVING, COMPUTE (typed operands in the expression), and DIVIDE…REMAINDER. Each
        // typed sender materializes to its digit window and each typed receiver decodes back after the byte op —
        // so every result is byte-identical to the byte path.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDARI.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC 9(5) VALUE 100.
            01 WS-B PIC 9(5) VALUE 30.
            01 WS-C PIC 9(5) VALUE 0.
            01 WS-R PIC 9(5) VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                ADD WS-B TO WS-A.
                DISPLAY WS-A.
                SUBTRACT 5 FROM WS-A.
                DISPLAY WS-A.
                MULTIPLY WS-B BY 2 GIVING WS-C.
                DISPLAY WS-C.
                DIVIDE WS-A BY WS-B GIVING WS-C.
                DISPLAY WS-C.
                COMPUTE WS-C = WS-A + WS-B.
                DISPLAY WS-C.
                DIVIDE WS-A BY WS-B GIVING WS-C REMAINDER WS-R.
                DISPLAY WS-C.
                DISPLAY WS-R.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // A:100+30=130; 130-5=125; C:30*2=60; 125/30=4 (trunc); C:125+30=155; 125/30=4 rem 5.
        Assert.Equal("00130\n00125\n00060\n00004\n00155\n00004\n00005",
            typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NumericComp_FlipsToTypedLong_AcrossAllOps_ByteIdentical()
    {
        // S4: unsigned-integer COMP and BINARY items (with VALUE) flip to typed `long` too — verified (DEVLOG 416)
        // to store the value truncated to the PICTURE digit count (% 10^digits), exactly like DISPLAY, so the long
        // model is byte-identical across VALUE/MOVE (incl. high-order truncation)/DISPLAY/COMPARE/arithmetic. The
        // byte storage width (4) differs from the digit count (5), which the cells now keep distinct.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDCMP4.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-C  PIC 9(5) COMP   VALUE 100.
            01 WS-B  PIC 9(5) BINARY VALUE 30.
            01 WS-R  PIC 9(5) COMP   VALUE 0.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-C.
                ADD WS-B TO WS-C.
                DISPLAY WS-C.
                MOVE 1234567 TO WS-R.
                DISPLAY WS-R.
                IF WS-C = 130 THEN DISPLAY "EQ130" END-IF.
                IF WS-C > WS-B THEN DISPLAY "CGTB" END-IF.
                COMPUTE WS-R = WS-C * WS-B.
                DISPLAY WS-R.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // C:100; +30=130; R: 1234567 → low5 "34567"; C=130 EQ; C>B; R: 130*30=3900 → "03900".
        Assert.Equal("00100\n00130\n34567\nEQ130\nCGTB\n03900",
            typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void SignedScaledNumeric_FlipsToTypedDecimal_AcrossOps_ByteIdentical()
    {
        // S4: signed and/or scaled numeric items (with VALUE) flip to a typed .NET `decimal`. The field init and
        // every op (DISPLAY with sign overpunch + implied-decimal scale, ADD, COMPUTE, COMPARE, MOVE-literal) route
        // through the EXACT byte codec (materialize→Encode/Decode / GetDisplayString), so the result is
        // byte-identical to the byte path — sign overpunch, scaling, and truncation included. (Field→field MOVE of
        // a decimal is not in this slice; it is loud-guarded.)
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDDEC.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(3)V99 VALUE 1.50.
            01 WS-B PIC S9(3)V99 VALUE 2.25.
            01 WS-C PIC S9(3)V99 VALUE 0.
            01 WS-N PIC S9(5)    VALUE -42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY WS-A.
                DISPLAY WS-N.
                ADD WS-A TO WS-B.
                DISPLAY WS-B.
                COMPUTE WS-C = WS-B - WS-A.
                DISPLAY WS-C.
                IF WS-A < WS-B THEN DISPLAY "ALTB" END-IF.
                MOVE 3.7 TO WS-C.
                DISPLAY WS-C.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // 1.50→"0015{"; -42→"0004K"; 1.50+2.25=3.75→"0037E"; 3.75-1.50=2.25→"0022E"; 3.7→3.70→"0037{".
        Assert.Equal("0015{\n0004K\n0037E\n0022E\nALTB\n0037{",
            typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void NumericFieldMove_AllLongDecimalCombos_ByteIdentical()
    {
        // S4: typed numeric field→field MOVE for every long/decimal combination — decimal→decimal, long→decimal,
        // decimal→long. A decimal on either end routes through the destination byte codec (encode the source value
        // into a dst-shaped window, decode back, store) so the dst's sign/scale/truncation matches the byte
        // MoveNumeric exactly. (long→long keeps its faster mod path, covered by NumericToNumeric_FieldMove.)
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDDMV.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-A PIC S9(3)V99 VALUE 1.50.
            01 WS-C PIC S9(3)V99 VALUE 0.
            01 WS-L PIC 9(5)     VALUE 42.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE WS-A TO WS-C.
                DISPLAY WS-C.
                MOVE WS-L TO WS-C.
                DISPLAY WS-C.
                MOVE WS-A TO WS-L.
                DISPLAY WS-L.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // decimal→decimal 1.50→"0015{"; long→decimal 42→42.00→"0420{"; decimal→long 1.50→trunc→1→"00001".
        Assert.Equal("0015{\n0420{\n00001", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MixedGroup_FlipsToRecordStruct_WithCharLongAndDecimalMembers_ByteIdentical()
    {
        // S3b/S4: a `01` group whose children mix character, unsigned-integer, and signed/scaled numeric items
        // flips to a .NET record struct with `string` / `long` / `decimal` members respectively. Each member's ops
        // (DISPLAY, MOVE, arithmetic, COMPARE) route through the same InstanceName-aware typed cells as a standalone
        // field, so the whole record is byte-identical to the byte path.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDGRPN.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 CUSTOMER.
               05 CUST-NAME PIC X(6)     VALUE "ACME".
               05 CUST-QTY  PIC 9(5)     VALUE 100.
               05 CUST-BAL  PIC S9(3)V99 VALUE 12.50.
            PROCEDURE DIVISION.
            MAIN-PARA.
                DISPLAY CUST-NAME.
                DISPLAY CUST-QTY.
                DISPLAY CUST-BAL.
                ADD 5 TO CUST-QTY.
                DISPLAY CUST-QTY.
                ADD 1.25 TO CUST-BAL.
                DISPLAY CUST-BAL.
                MOVE "WIDGET" TO CUST-NAME.
                DISPLAY CUST-NAME.
                IF CUST-QTY = 105 THEN DISPLAY "QTYOK" END-IF.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        // string ACME→WIDGET; long 100→105; decimal 12.50→"0125{", +1.25=13.75→"0137E"; compare 105.
        Assert.Equal("ACME\n00100\n0125{\n00105\n0137E\nWIDGET\nQTYOK",
            typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }

    [Fact]
    public void MoveZeros_ToTypedNumericLongAndDecimal_ByteIdentical()
    {
        // Regression: MOVE ZEROS to a typed numeric field must store 0 (long) / 0m (decimal), NOT a fill string —
        // storing a string into a long/decimal field emitted invalid IL (InvalidProgramException). Byte-identical:
        // the byte path zero-fills the digit image, which DISPLAYs the same as a 0-valued long/decimal.
        const string program = """
            IDENTIFICATION DIVISION.
            PROGRAM-ID. TYPEDZ.
            DATA DIVISION.
            WORKING-STORAGE SECTION.
            01 WS-N PIC 9(5)     VALUE 123.
            01 WS-D PIC S9(3)V99 VALUE 9.99.
            PROCEDURE DIVISION.
            MAIN-PARA.
                MOVE ZEROS TO WS-N.
                DISPLAY WS-N.
                MOVE ZEROS TO WS-D.
                DISPLAY WS-D.
                STOP RUN.
            """;

        var typed = CompileAndRun(program, enableTypedFields: true);
        Assert.True(typed.success, typed.stderr);
        Assert.Equal("00000\n0000{", typed.stdout.Replace("\r\n", "\n"));

        var bytes = CompileAndRun(program);
        Assert.True(bytes.success, bytes.stderr);
        Assert.Equal(typed.stdout.Replace("\r\n", "\n"), bytes.stdout.Replace("\r\n", "\n"));
    }
}
