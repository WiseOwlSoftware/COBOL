      *>--- conformance-case
      *> id: g85_f34_05
      *> rules: [1989-FMT-A.2.6.2, 1989-AR-A.2.6.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.6.2 - FUNCTION ANNUITY ( argument-1 argument-2 ), the
      *> two arguments separated by a space (all lines). §1989:A.2.6.3
      *> AR2 - argument-1 >= 0: its boundary value zero, as a literal
      *> and as a data item, is accepted and §1989:A.2.6.4 RV1 gives 1 /
      *> argument-2 = 1 / 4 = 0.25 (lines 1-2); the positive rate 0.1
      *> with 2 periods takes §1989:A.2.6.4 RV2: 0.1 / (1 - 1.1 ** -2) =
      *> 0.1 / 0.17355371.. = 0.57619047.. (line 3). Each value is an
      *> approximation, pinned by a narrow range. A line fails if the
      *> format is refused, zero is rejected, or a value leaves its
      *> range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3405.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R     PIC S9V9(6).
       01  RATE  PIC 9V99 VALUE 0.
       01  NPER  PIC 99 VALUE 4.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION ANNUITY ( 0 4 ).
           IF R NOT < 0.249999 AND R NOT > 0.250001
               DISPLAY "ANN-ZERO-LIT OK"
           ELSE
               DISPLAY "ANN-ZERO-LIT BAD"
           END-IF.
           COMPUTE R = FUNCTION ANNUITY ( RATE NPER ).
           IF R NOT < 0.249999 AND R NOT > 0.250001
               DISPLAY "ANN-ZERO-ID OK"
           ELSE
               DISPLAY "ANN-ZERO-ID BAD"
           END-IF.
           MOVE 0.10 TO RATE.
           MOVE 2 TO NPER.
           COMPUTE R = FUNCTION ANNUITY ( RATE NPER ).
           IF R NOT < 0.576190 AND R NOT > 0.576191
               DISPLAY "ANN-RATE OK"
           ELSE
               DISPLAY "ANN-RATE BAD"
           END-IF.
           STOP RUN.
