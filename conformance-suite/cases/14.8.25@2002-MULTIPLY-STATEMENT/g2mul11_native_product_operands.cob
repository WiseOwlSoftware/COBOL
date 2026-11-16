      *>--- conformance-case
      *> id: g2mul11_native_product_operands
      *> rules: [2002-GR-14.8.25.3-1, 2002-GR-14.8.25.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> No ARITHMETIC clause, so native arithmetic is in effect
      *>   (§2002:11.8.4.2 GR3). Every product is exact and fits its
      *>   receiver, so no rounding, truncation or size error enters.
      *> §2002:14.8.25.3 GR1 (format 1) - the multiplier is literal-1
      *>   or identifier-1, the multiplicand identifier-2, and the
      *>   product is stored as the new value of identifier-2:
      *>   F1 working storage: 6 BY A (7) -> 0042; M (2) BY B (5) ->
      *>      0010; -1.5 BY C (2.5) -> -3.75 (signed, two decimals).
      *>   F2 several identifier-2: 3 BY A (42) B (10) -> 0126 0030,
      *>      each its own product with the one multiplier.
      *>   F3 communication-section operands: CN (9(5) in the record
      *>      that redefines the input CD area, §2002:A.1.2.2 SR5) as
      *>      identifier-2 (7 x 6 = 42 -> 00042), and CM (2) BY the
      *>      CD's TEXT LENGTH TL (5, §2002:A.1.2.3 GR2) -> 0010.
      *> §2002:14.8.25.3 GR2 (format 2) - the product of literal-1 or
      *>   identifier-1 and literal-2 or identifier-2 is stored in each
      *>   identifier-3; identifier-1 and identifier-2 are unchanged:
      *>   G1 working storage: 12 BY 3 GIVING A -> 0036; A (36) BY M
      *>      (2) GIVING B (9(4)) and WE (numeric-edited -99.99) ->
      *>      B=0072 WE=[ 72.00]; A, M unchanged (0036, 2).
      *>   G2 communication-section operands: CN (7) BY TL (5) GIVING
      *>      CP 9(4) and CQ ZZZ9 (record items) -> 0035, "  35";
      *>      CN, TL unchanged (00007, 0005).
      *> Each leg fails if the product is stored in the wrong operand,
      *> a CD operand is refused or read from the wrong positions, or
      *> identifier-1/-2 of format 2 is altered.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MUL11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M   PIC 9 VALUE 2.
       01 A   PIC 9(4) VALUE 7.
       01 B   PIC 9(4) VALUE 5.
       01 C   PIC S9V99 VALUE 2.5.
       01 CE  PIC -9.99.
       01 WE  PIC -99.99.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL.
       01 INREC.
          05 FILLER PIC X(12).
          05 CN     PIC 9(5).
          05 CM     PIC 9.
          05 CP     PIC 9(4).
          05 CQ     PIC ZZZ9.
          05 FILLER PIC X(61).
       PROCEDURE DIVISION.
       MAIN.
           MULTIPLY 6 BY A.
           MULTIPLY M BY B.
           MULTIPLY -1.5 BY C.
           MOVE C TO CE.
           DISPLAY "F1 A=" A " B=" B " C=" CE.
           MULTIPLY 3 BY A B.
           DISPLAY "F2 A=" A " B=" B.
           MOVE 7 TO CN.
           MULTIPLY 6 BY CN.
           MOVE 2 TO CM.
           MOVE 5 TO TL.
           MULTIPLY CM BY TL.
           DISPLAY "F3 CN=" CN " TL=" TL.
           MULTIPLY 12 BY 3 GIVING A.
           DISPLAY "G1 A=" A.
           MULTIPLY A BY M GIVING B WE.
           DISPLAY "G1 B=" B " WE=[" WE "] A=" A " M=" M.
           MOVE 7 TO CN.
           MOVE 5 TO TL.
           MULTIPLY CN BY TL GIVING CP CQ.
           DISPLAY "G2 CP=" CP " CQ=[" CQ "] CN=" CN " TL=" TL.
           STOP RUN.
