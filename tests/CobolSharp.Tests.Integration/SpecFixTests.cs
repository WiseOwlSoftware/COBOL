// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.

using Xunit;

namespace CobolSharp.Tests.Integration;

/// <summary>
/// Regression tests for compiler bugs from docs/SPEC_FIX_BACKLOG.md, re-implemented on main after the
/// worktree-isolated fix workflow produced diffs against a stale base (see DEVLOG 334). Each [Fact] is CLI-verified.
/// </summary>
public sealed class SpecFixTests : EndToEndTestBase
{
    // ISO §15.18 — FUNCTION CONCAT is the synonym of CONCATENATE (added to the alphanumeric-function set; it was
    // typed numeric and crashed with InvalidCastException when used).
    [Fact]
    public void Concat_IsAlphanumeric_ReturnsConcatenation()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. CONCATT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-X PIC X(8) VALUE SPACES.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE FUNCTION CONCAT(\"AB\", \"CD\") TO WS-X.\n" +
            "           DISPLAY WS-X.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("ABCD", stdout);
    }

    // ISO §13.18.3 rule 27 — only A,B,C,D,E,N,P,R,S,V,X,Z are forbidden as the currency PICTURE SYMBOL; other
    // letters (e.g. 'U', as in the spec's own EUR/USD examples) are valid. CBL3124 used to reject every letter.
    [Fact]
    public void CurrencySign_NonReservedLetterSymbol_IsAccepted()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. CURU.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       CONFIGURATION SECTION.\n" +
            "       SPECIAL-NAMES.\n" +
            "           CURRENCY SIGN IS \"$\" WITH PICTURE SYMBOL \"U\".\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-C PIC U99 VALUE 42.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           DISPLAY WS-C.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("$42", stdout);
    }

    // ISO §13.18.3 — BLANK WHEN ZERO with a zero value yields a field of spaces of the PICTURE width; the display
    // path used to TrimEnd that all-blank field down to an empty string.
    [Fact]
    public void BlankWhenZero_ZeroValue_RendersBlankField()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. BWZT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-B PIC ZZ,ZZ9 BLANK WHEN ZERO.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE 0 TO WS-B.\n" +
            "           DISPLAY \"[\" WS-B \"]\".\n" +
            "           MOVE 123 TO WS-B.\n" +
            "           DISPLAY \"[\" WS-B \"]\".\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        // zero → 6-char blank field (ZZ,ZZ9); non-zero → normal edit.
        Assert.Equal("[      ]\r\n[   123]", stdout);
    }

    // ISO §15 — variadic string functions accept SPACE-separated literal arguments (each space before a literal
    // begins a new argument); previously only the first argument was passed (the rest were swallowed).
    [Fact]
    public void Concatenate_SpaceSeparatedLiteralArgs_PassesAll()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. VARCAT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-X PIC X(10) VALUE SPACES.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE FUNCTION CONCATENATE(\"ab\" \"cd\" \"ef\") TO WS-X.\n" +
            "           DISPLAY \"[\" WS-X \"]\".\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("[abcdef]", stdout);
    }

    // ISO §7.2.3 — COPY with a quoted-literal text-name (the literal-1 alternative). The reader used to stop at
    // the opening quote and resolve an empty name → "copybook not found".
    [Fact]
    public void Copy_QuotedLiteralTextName_Resolves()
    {
        File.WriteAllText(Path.Combine(_tempDir, "MYBOOK.cpy"),
            "       01 GREETING PIC X(8) VALUE \"HI THERE\".\n");
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. CPYLIT.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       COPY \"MYBOOK\".\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           DISPLAY GREETING.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("HI THERE", stdout);
    }

    // ISO §8.4.2.3 — a reference-modification operand is category alphanumeric. FUNCTION UPPER-CASE(X(1:4)) used
    // to return 0 (the substring was sent through the numeric arg path and decoded as a decimal); it now reads
    // the substring as a string.
    [Fact]
    public void RefModdedAlphanumericFunctionArg_ReadsAsString()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. RMARG.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-IN  PIC X(8) VALUE \"abcdefgh\".\n" +
            "       01 WS-OUT PIC X(4).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE FUNCTION UPPER-CASE(WS-IN(1:4)) TO WS-OUT.\n" +
            "           DISPLAY WS-OUT.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("ABCD", stdout);
    }

    // ISO §8.5.1.2 — COMP-1/COMP-2 are floating-point; arithmetic into them must not truncate the fraction to a
    // fixed-point scale. StoreArithmeticResult was scaling/rounding to the receiver's FractionDigits (0 for a
    // PIC-less float), so COMPUTE WS-F = 1.0/3.0 → 0 and 3.14159*2 → 6.
    [Fact]
    public void Compute_IntoFloatReceiver_PreservesFraction()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. COMPF.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-D  USAGE COMP-2.\n" +
            "       01 WS-O1 PIC 9V9(8).\n" +
            "       01 WS-O2 PIC 99V9(5).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           COMPUTE WS-D = 1.0 / 3.0.\n" +
            "           MOVE WS-D TO WS-O1.\n" +
            "           DISPLAY \"DIV=\" WS-O1.\n" +
            "           COMPUTE WS-D = 3.14159 * 2.\n" +
            "           MOVE WS-D TO WS-O2.\n" +
            "           DISPLAY \"MUL=\" WS-O2.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("DIV=033333333\r\nMUL=0628318", stdout);
    }

    // ISO §13.18.35 — raw DISPLAY of a COMP-1/COMP-2 (binary floating-point) item shows its natural decimal
    // magnitude (shortest round-trip; integral → no point), not the synthetic 18-digit fixed-point integer.
    [Fact]
    public void Display_OfFloatItem_ShowsNaturalMagnitude()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. COMPD.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-D USAGE COMP-2.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           COMPUTE WS-D = 3.14159 * 2.\n" +
            "           DISPLAY \"A=\" WS-D.\n" +
            "           COMPUTE WS-D = 100 * 3.\n" +
            "           DISPLAY \"B=\" WS-D.\n" +
            "           COMPUTE WS-D = -2.5 / 4.\n" +
            "           DISPLAY \"C=\" WS-D.\n" +
            "           MOVE ZERO TO WS-D.\n" +
            "           DISPLAY \"D=\" WS-D.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("A=6.28318\r\nB=300\r\nC=-0.625\r\nD=0", stdout);
    }

    // ISO §7.2.3.4 GR 9 b — COPY … REPLACING LEADING/TRAILING ==partial== BY ==partial== (partial-word
    // substitution). Was unimplemented: the LEADING/TRAILING keyword was mis-read as an operand → no substitution.
    [Fact]
    public void Copy_ReplacingLeadingAndTrailing_PartialWord()
    {
        File.WriteAllText(Path.Combine(_tempDir, "BK.cpy"),
            "       01 PREFIX-FLD PIC X(5) VALUE \"HELLO\".\n" +
            "       01 ITEM-OLD   PIC X(5) VALUE \"WORLD\".\n");
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. REPL.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       COPY \"BK\" REPLACING LEADING ==PREFIX== BY ==XQ==\n" +
            "                           TRAILING ==-OLD== BY ==-NEW==.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           DISPLAY \"A=\" XQ-FLD.\n" +
            "           DISPLAY \"B=\" ITEM-NEW.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("A=HELLO\r\nB=WORLD", stdout);
    }

    // ISO §13.18.60 — COMP-4 / COMPUTATIONAL-4 is the conventional vendor synonym for BINARY/COMPUTATIONAL.
    // There was no COMP_4 lexer token, so `… COMP-4` lexed as an IDENTIFIER, was swallowed by the generic
    // (vendor) data clause, and the item silently became USAGE DISPLAY. Now it binds as UsageKind.Binary —
    // exercised here in both the bare form (COMP-4) and the USAGE IS form (COMPUTATIONAL-4).
    [Fact]
    public void Comp4_IsBinarySynonym_StoresAndComputes()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. COMP4T.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-A PIC 9(7) COMP-4 VALUE 1234567.\n" +
            "       01 WS-B PIC 9(7) USAGE IS COMPUTATIONAL-4.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           ADD 1 TO WS-A.\n" +
            "           MOVE WS-A TO WS-B.\n" +
            "           DISPLAY WS-B.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("1234568", stdout);
    }

    // ISO §13.18.60.2 — an unknown COMP-n (here COMP-9) is not a defined USAGE. It used to lex as an
    // IDENTIFIER, be absorbed by the generic vendor-clause, and silently become USAGE DISPLAY; it is now a
    // hard error (CBL0816) rather than a silently-wrong storage class.
    [Fact]
    public void CompNine_IsRejected_HardDiagnostic()
    {
        var (ok, _, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. COMP9T.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-X PIC 9(4) COMP-9.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           STOP RUN.\n");
        Assert.False(ok);
        Assert.Contains("CBL0816", stderr);
    }

    // ISO §14.9.24.4 GR7b/GR12b — a variable-length-record MERGE … GIVING writes each output record at the
    // length it had when read, not the SD maximum. MergeRecordsInternal stored every input record at the full
    // SD buffer size (discarding the actual read length); it now sizes each to FileRuntime.GetLastRecordLength.
    [Fact]
    public void MergeVaryingRecord_Giving_PreservesPerRecordLength()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. MRGVL.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT IN1 ASSIGN TO \"mrgv1\".\n" +
            "           SELECT IN2 ASSIGN TO \"mrgv2\".\n" +
            "           SELECT OUTF ASSIGN TO \"mrgvout\".\n" +
            "           SELECT MRGF ASSIGN TO \"mrgvwk\".\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       FD IN1\n" +
            "          RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS DEPENDING ON W1-LEN.\n" +
            "       01 IN1-REC PIC X(10).\n" +
            "       FD IN2\n" +
            "          RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS DEPENDING ON W2-LEN.\n" +
            "       01 IN2-REC PIC X(10).\n" +
            "       FD OUTF\n" +
            "          RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS DEPENDING ON O-LEN.\n" +
            "       01 OUT-REC PIC X(10).\n" +
            "       SD MRGF\n" +
            "          RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS DEPENDING ON M-LEN.\n" +
            "       01 MRG-REC.\n" +
            "          05 M-KEY  PIC X(1).\n" +
            "          05 M-REST PIC X(9).\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 W1-LEN PIC 9(2).\n" +
            "       01 W2-LEN PIC 9(2).\n" +
            "       01 O-LEN  PIC 9(2).\n" +
            "       01 M-LEN  PIC 9(2).\n" +
            "       01 WS-EOF PIC X VALUE \"N\".\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           OPEN OUTPUT IN1.\n" +
            "           MOVE \"Bxx\" TO IN1-REC. MOVE 3 TO W1-LEN. WRITE IN1-REC.\n" +
            "           MOVE \"Dyyyyyyyyy\" TO IN1-REC. MOVE 10 TO W1-LEN. WRITE IN1-REC.\n" +
            "           CLOSE IN1.\n" +
            "           OPEN OUTPUT IN2.\n" +
            "           MOVE \"Az\" TO IN2-REC. MOVE 2 TO W2-LEN. WRITE IN2-REC.\n" +
            "           MOVE \"Cwwww\" TO IN2-REC. MOVE 5 TO W2-LEN. WRITE IN2-REC.\n" +
            "           CLOSE IN2.\n" +
            "           MERGE MRGF ON ASCENDING KEY M-KEY\n" +
            "               USING IN1 IN2 GIVING OUTF.\n" +
            "           OPEN INPUT OUTF.\n" +
            "           PERFORM UNTIL WS-EOF = \"Y\"\n" +
            "               READ OUTF\n" +
            "                   AT END MOVE \"Y\" TO WS-EOF\n" +
            "                   NOT AT END\n" +
            "                       DISPLAY \"[\" OUT-REC(1:O-LEN) \"] LEN=\" O-LEN\n" +
            "               END-READ\n" +
            "           END-PERFORM.\n" +
            "           CLOSE OUTF.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        // Merged ascending by M-KEY; each record retains its source length (not padded to the SD max of 10).
        Assert.Equal(
            "[Az] LEN=02\r\n[Bxx] LEN=03\r\n[Cwwww] LEN=05\r\n[Dyyyyyyyyy] LEN=10",
            stdout);
    }

    // ISO §9.1.5(2), §8.4.6.2 — a contained program shares a containing program's FD … IS GLOBAL file
    // connector. GFDOUT opens an indexed GLOBAL file and CALLs the contained GFDIN, which reads it by its
    // (inherited) prime RECORD KEY without its own OPEN. The prime key lives in the global record, so it is
    // inherited — INDEXED works once CBL0702 (file-not-open) is not fatal for a global file.
    [Fact]
    public void NestedProgram_ReadsContainingGlobalIndexedFile_SharesConnector()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. GFDOUT.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT IXF ASSIGN TO \"gfd.dat\"\n" +
            "               ORGANIZATION IS INDEXED\n" +
            "               ACCESS MODE IS DYNAMIC\n" +
            "               RECORD KEY IS IX-KEY.\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       FD  IXF IS GLOBAL.\n" +
            "       01  IX-REC.\n" +
            "           05 IX-KEY  PIC X(3).\n" +
            "           05 IX-VAL  PIC X(5).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           OPEN OUTPUT IXF.\n" +
            "           MOVE \"A01\" TO IX-KEY.\n" +
            "           MOVE \"HELLO\" TO IX-VAL.\n" +
            "           WRITE IX-REC INVALID KEY DISPLAY \"WERR\".\n" +
            "           CLOSE IXF.\n" +
            "           OPEN INPUT IXF.\n" +
            "           CALL \"GFDIN\".\n" +
            "           CLOSE IXF.\n" +
            "           STOP RUN.\n" +
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. GFDIN.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       SUB-PARA.\n" +
            "           MOVE \"A01\" TO IX-KEY.\n" +
            "           READ IXF KEY IS IX-KEY\n" +
            "               INVALID KEY DISPLAY \"NOTFOUND\".\n" +
            "           DISPLAY IX-VAL.\n" +
            "           EXIT PROGRAM.\n" +
            "       END PROGRAM GFDIN.\n" +
            "       END PROGRAM GFDOUT.\n");
        Assert.True(ok, stderr);
        Assert.Equal("HELLO", stdout);
    }

    // ISO §9.1.5(2) — same as above but RELATIVE. The RELATIVE KEY is a separate WORKING-STORAGE item, not
    // subordinate to the global record, so the FD's GLOBAL clause does not make it global; the contained
    // program could not resolve it. The Layer-2 fix inherits the relative-key item (sharing the container's
    // storage) so the contained program's keyed READ drives the shared connector.
    [Fact]
    public void NestedProgram_ReadsContainingGlobalRelativeFile_SharesConnectorAndKey()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. GFDROUT.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT RLF ASSIGN TO \"gfdr.dat\"\n" +
            "               ORGANIZATION IS RELATIVE\n" +
            "               ACCESS MODE IS DYNAMIC\n" +
            "               RELATIVE KEY IS RL-KEY.\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       FD  RLF IS GLOBAL.\n" +
            "       01  RL-REC.\n" +
            "           05 RL-VAL  PIC X(5).\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01  RL-KEY PIC 9(2).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           OPEN OUTPUT RLF.\n" +
            "           MOVE 2 TO RL-KEY.\n" +
            "           MOVE \"WORLD\" TO RL-VAL.\n" +
            "           WRITE RL-REC INVALID KEY DISPLAY \"WERR\".\n" +
            "           CLOSE RLF.\n" +
            "           OPEN INPUT RLF.\n" +
            "           CALL \"GFDRIN\".\n" +
            "           CLOSE RLF.\n" +
            "           STOP RUN.\n" +
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. GFDRIN.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       SUB-PARA.\n" +
            "           MOVE 2 TO RL-KEY.\n" +
            "           READ RLF\n" +
            "               INVALID KEY DISPLAY \"NOTFOUND\".\n" +
            "           DISPLAY RL-VAL.\n" +
            "           EXIT PROGRAM.\n" +
            "       END PROGRAM GFDRIN.\n" +
            "       END PROGRAM GFDROUT.\n");
        Assert.True(ok, stderr);
        Assert.Equal("WORLD", stdout);
    }

    // ISO §13.18.43 GR13a/GR15 — an explicit RELEASE/RETURN through a variable-length SD must preserve each
    // record's own length: RELEASE stores the bytes the DEPENDING ON item indicates, and RETURN restores that
    // length into the DEPENDING ON item. Previously LowerRelease/LowerReturn always used the SD max length, so
    // every returned record came back padded to the maximum and the DEPENDING item was never updated. (The
    // 1-char key stays within the minimum record length, per the SORT key rule.)
    [Fact]
    public void SortVaryingRecord_ReleaseReturn_PreservesPerRecordLength()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. SRTVL.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT SORT-FILE ASSIGN TO \"srtvlwk\".\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       SD SORT-FILE\n" +
            "          RECORD IS VARYING IN SIZE FROM 1 TO 5 CHARACTERS DEPENDING ON WS-LEN.\n" +
            "       01 SORT-REC.\n" +
            "          05 S-KEY  PIC X(1).\n" +
            "          05 S-REST PIC X(4).\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-LEN PIC 9(2).\n" +
            "       01 WS-EOF PIC X VALUE \"N\".\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           SORT SORT-FILE ON ASCENDING KEY S-KEY\n" +
            "               INPUT PROCEDURE IS FILL-PARA\n" +
            "               OUTPUT PROCEDURE IS DRAIN-PARA.\n" +
            "           STOP RUN.\n" +
            "       FILL-PARA.\n" +
            "           MOVE \"AZZZZ\" TO SORT-REC. MOVE 5 TO WS-LEN. RELEASE SORT-REC.\n" +
            "           MOVE \"B\"     TO SORT-REC. MOVE 1 TO WS-LEN. RELEASE SORT-REC.\n" +
            "           MOVE \"CYY\"   TO SORT-REC. MOVE 3 TO WS-LEN. RELEASE SORT-REC.\n" +
            "       DRAIN-PARA.\n" +
            "           PERFORM UNTIL WS-EOF = \"Y\"\n" +
            "               RETURN SORT-FILE\n" +
            "                   AT END MOVE \"Y\" TO WS-EOF\n" +
            "                   NOT AT END\n" +
            "                       DISPLAY \"[\" SORT-REC(1:WS-LEN) \"] LEN=\" WS-LEN\n" +
            "               END-RETURN\n" +
            "           END-PERFORM.\n");
        Assert.True(ok, stderr);
        // Sorted ascending by S-KEY (A<B<C); each record restored at its own released length.
        Assert.Equal("[AZZZZ] LEN=05\r\n[B] LEN=01\r\n[CYY] LEN=03", stdout);
    }

    // ISO §15.x — FUNCTION SUM(T(ALL)) over an OCCURS DEPENDING ON table ranges over the CURRENT depending
    // value, not the OCCURS maximum. ExpandAllSubscript expanded to the max (summing inactive tail slots); it
    // now masks each occurrence beyond the minimum by MAX(0, MIN(1, N-(idx-1))) so inactive slots add 0.
    [Fact]
    public void SumAllSubscript_OverOccursDependingOn_UsesCurrentLength()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. SUMODO.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 N        PIC 9 VALUE 5.\n" +
            "       01 TBL.\n" +
            "          05 T PIC 9 OCCURS 1 TO 5 DEPENDING ON N.\n" +
            "       01 WS-R     PIC 9(3).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE 1 TO T(1).\n" +
            "           MOVE 2 TO T(2).\n" +
            "           MOVE 3 TO T(3).\n" +
            "           MOVE 9 TO T(4).\n" +
            "           MOVE 9 TO T(5).\n" +
            "           MOVE 3 TO N.\n" +
            "           COMPUTE WS-R = FUNCTION SUM(T(ALL)).\n" +
            "           DISPLAY WS-R.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("006", stdout);   // 1+2+3 = 6, not 1+2+3+9+9 = 24
    }

    // ISO §14.9.40 (Format-2 table SORT, COBOL-2002) — an elementary OCCURS item sorted on ITSELF: the key is
    // the table item, whose storage Length spans the whole table. BuildKeySpecField now clamps the key length to
    // one entry, so the runtime keys each entry within bounds (was an out-of-range throw).
    [Fact]
    public void SortTable_ElementarySelfKey_SortsInPlace()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. TSELF.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-TBL.\n" +
            "          05 WS-N PIC 9(3) OCCURS 5 TIMES.\n" +
            "       01 WS-I PIC 9 VALUE 1.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE 300 TO WS-N(1).\n" +
            "           MOVE 100 TO WS-N(2).\n" +
            "           MOVE 500 TO WS-N(3).\n" +
            "           MOVE 200 TO WS-N(4).\n" +
            "           MOVE 400 TO WS-N(5).\n" +
            "           SORT WS-N ON ASCENDING KEY WS-N.\n" +
            "           PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 5\n" +
            "               DISPLAY WS-N(WS-I)\n" +
            "           END-PERFORM.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("100\r\n200\r\n300\r\n400\r\n500", stdout);
    }

    // ISO §14.9.40.4 GR23 — Format-2 table SORT with the KEY data-name omitted: the table item is itself the key
    // (DESCENDING here also exercises the direction wiring).
    [Fact]
    public void SortTable_OmittedKey_UsesTableItemDescending()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. TSELF2.\n" +
            "       DATA DIVISION.\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-TBL.\n" +
            "          05 WS-N PIC 9(3) OCCURS 3 TIMES.\n" +
            "       01 WS-I PIC 9 VALUE 1.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN-PARA.\n" +
            "           MOVE 020 TO WS-N(1).\n" +
            "           MOVE 050 TO WS-N(2).\n" +
            "           MOVE 010 TO WS-N(3).\n" +
            "           SORT WS-N ON DESCENDING KEY.\n" +
            "           PERFORM VARYING WS-I FROM 1 BY 1 UNTIL WS-I > 3\n" +
            "               DISPLAY WS-N(WS-I)\n" +
            "           END-PERFORM.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("050\r\n020\r\n010", stdout);
    }

    // ISO §14.9.30 GR21 d.3 (COBOL-2002 READ … PREVIOUS) — a READ PREVIOUS immediately after OPEN INPUT, with no
    // file position indicator yet established, raises the AT END condition (status 10). It used to return the
    // highest-key record.
    [Fact]
    public void ReadPrevious_AfterOpen_RaisesAtEnd()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. RDPREV1.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT F ASSIGN TO \"rp1.dat\"\n" +
            "               ORGANIZATION IS INDEXED\n" +
            "               ACCESS MODE IS DYNAMIC\n" +
            "               RECORD KEY IS F-KEY\n" +
            "               FILE STATUS IS WS-ST.\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       FD F.\n" +
            "       01 F-REC.\n" +
            "          05 F-KEY PIC 9(2).\n" +
            "          05 F-FILLER PIC X(8).\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-ST PIC XX.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN.\n" +
            "           OPEN OUTPUT F.\n" +
            "           MOVE 10 TO F-KEY. WRITE F-REC.\n" +
            "           MOVE 20 TO F-KEY. WRITE F-REC.\n" +
            "           CLOSE F.\n" +
            "           OPEN INPUT F.\n" +
            "           READ F PREVIOUS RECORD\n" +
            "               AT END DISPLAY \"ATEND \" WS-ST\n" +
            "               NOT AT END DISPLAY \"GOT \" F-KEY\n" +
            "           END-READ.\n" +
            "           CLOSE F.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("ATEND 10", stdout);   // at-end status 10, not the highest key
    }

    // ISO §14.9.30 GR21 d.2 — after START KEY = EQUAL k, the first READ PREVIOUS returns the record at the file
    // position indicator (key ≤ k, i.e. k itself), not its strict predecessor.
    [Fact]
    public void ReadPrevious_AfterStartEqual_ReturnsTheEqualKey()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. RDPREV2.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT F ASSIGN TO \"rp2.dat\"\n" +
            "               ORGANIZATION IS INDEXED\n" +
            "               ACCESS MODE IS DYNAMIC\n" +
            "               RECORD KEY IS F-KEY\n" +
            "               FILE STATUS IS WS-ST.\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       FD F.\n" +
            "       01 F-REC.\n" +
            "          05 F-KEY PIC 9(2).\n" +
            "          05 F-FILLER PIC X(8).\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-ST PIC XX.\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN.\n" +
            "           OPEN OUTPUT F.\n" +
            "           MOVE 10 TO F-KEY. WRITE F-REC.\n" +
            "           MOVE 20 TO F-KEY. WRITE F-REC.\n" +
            "           MOVE 30 TO F-KEY. WRITE F-REC.\n" +
            "           CLOSE F.\n" +
            "           OPEN INPUT F.\n" +
            "           MOVE 20 TO F-KEY.\n" +
            "           START F KEY IS EQUAL TO F-KEY\n" +
            "               INVALID KEY DISPLAY \"BADSTART\"\n" +
            "           END-START.\n" +
            "           READ F PREVIOUS RECORD\n" +
            "               AT END DISPLAY \"ATEND\"\n" +
            "               NOT AT END DISPLAY \"GOT \" F-KEY\n" +
            "           END-READ.\n" +
            "           CLOSE F.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("GOT 20", stdout);   // the equal key, not its predecessor (10)
    }

    // ISO §14.9.30 GR21 d.1 — the RELATIVE-file analog of ReadPrevious_AfterOpen_RaisesAtEnd: a READ PREVIOUS
    // immediately after OPEN INPUT (no file position indicator established) raises AT END (status 10) instead of
    // returning the highest-numbered relative record.
    [Fact]
    public void ReadPrevious_Relative_AfterOpen_RaisesAtEnd()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. RDPREVR1.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT F ASSIGN TO \"rpr1.dat\"\n" +
            "               ORGANIZATION IS RELATIVE\n" +
            "               ACCESS MODE IS DYNAMIC\n" +
            "               RELATIVE KEY IS RL-KEY\n" +
            "               FILE STATUS IS WS-ST.\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       FD F.\n" +
            "       01 F-REC.\n" +
            "          05 F-VAL PIC 9(2).\n" +
            "          05 F-FILLER PIC X(8).\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-ST PIC XX.\n" +
            "       01 RL-KEY PIC 9(2).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN.\n" +
            "           OPEN OUTPUT F.\n" +
            "           MOVE 1 TO RL-KEY. MOVE 11 TO F-VAL. WRITE F-REC.\n" +
            "           MOVE 2 TO RL-KEY. MOVE 22 TO F-VAL. WRITE F-REC.\n" +
            "           CLOSE F.\n" +
            "           OPEN INPUT F.\n" +
            "           READ F PREVIOUS RECORD\n" +
            "               AT END DISPLAY \"ATEND \" WS-ST\n" +
            "               NOT AT END DISPLAY \"GOT \" F-VAL\n" +
            "           END-READ.\n" +
            "           CLOSE F.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("ATEND 10", stdout);   // at-end status 10, not the highest relative record
    }

    // ISO §14.9.30 GR21 d.2 — the RELATIVE-file analog of ReadPrevious_AfterStartEqual_ReturnsTheEqualKey: after
    // START KEY = EQUAL n, the first READ PREVIOUS returns the record at the file position indicator (slot n
    // itself), not the strict predecessor slot. The relative Start() sets _currentRecord = slot-1 for the NEXT
    // hack, so without the _startPositioned flag READ PREVIOUS would skip both slot n and slot n-1.
    [Fact]
    public void ReadPrevious_Relative_AfterStartEqual_ReturnsTheEqualSlot()
    {
        var (ok, stdout, stderr) = CompileAndRun(
            "       IDENTIFICATION DIVISION.\n" +
            "       PROGRAM-ID. RDPREVR2.\n" +
            "       ENVIRONMENT DIVISION.\n" +
            "       INPUT-OUTPUT SECTION.\n" +
            "       FILE-CONTROL.\n" +
            "           SELECT F ASSIGN TO \"rpr2.dat\"\n" +
            "               ORGANIZATION IS RELATIVE\n" +
            "               ACCESS MODE IS DYNAMIC\n" +
            "               RELATIVE KEY IS RL-KEY\n" +
            "               FILE STATUS IS WS-ST.\n" +
            "       DATA DIVISION.\n" +
            "       FILE SECTION.\n" +
            "       FD F.\n" +
            "       01 F-REC.\n" +
            "          05 F-VAL PIC 9(2).\n" +
            "          05 F-FILLER PIC X(8).\n" +
            "       WORKING-STORAGE SECTION.\n" +
            "       01 WS-ST PIC XX.\n" +
            "       01 RL-KEY PIC 9(2).\n" +
            "       PROCEDURE DIVISION.\n" +
            "       MAIN.\n" +
            "           OPEN OUTPUT F.\n" +
            "           MOVE 1 TO RL-KEY. MOVE 11 TO F-VAL. WRITE F-REC.\n" +
            "           MOVE 2 TO RL-KEY. MOVE 22 TO F-VAL. WRITE F-REC.\n" +
            "           MOVE 3 TO RL-KEY. MOVE 33 TO F-VAL. WRITE F-REC.\n" +
            "           CLOSE F.\n" +
            "           OPEN INPUT F.\n" +
            "           MOVE 2 TO RL-KEY.\n" +
            "           START F KEY IS EQUAL TO RL-KEY\n" +
            "               INVALID KEY DISPLAY \"BADSTART\"\n" +
            "           END-START.\n" +
            "           READ F PREVIOUS RECORD\n" +
            "               AT END DISPLAY \"ATEND\"\n" +
            "               NOT AT END DISPLAY \"GOT \" F-VAL\n" +
            "           END-READ.\n" +
            "           CLOSE F.\n" +
            "           STOP RUN.\n");
        Assert.True(ok, stderr);
        Assert.Equal("GOT 22", stdout);   // the equal slot, not its predecessor (11)
    }
}
