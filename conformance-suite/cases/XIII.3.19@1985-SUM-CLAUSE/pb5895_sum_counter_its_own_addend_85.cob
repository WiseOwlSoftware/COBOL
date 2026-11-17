      *>--- conformance-case
      *> id: pb5895_sum_counter_its_own_addend_85
      *> rules: [1985-GR-XIII.3.19.4-9]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5895. 1985:XIII.3.19.4 GR9a: "When one of the
      *> addends is the sum counter defined by the data description
      *> entry in which that SUM clause appears, the initial value of
      *> that sum counter at the time of summation is used in the
      *> summing operation", and crossfooting runs in the order the
      *> counters are defined. XIII.3.19.3 SR1 admits a sum counter of
      *> the same report group as an addend, and no 1985 rule forbids
      *> a self addend or two counters that sum each other (the 2002
      *> and later SR4e refusal is not in this edition).
      *> DERIVATION. Footing 1 subtotals WS-A (1 + 2 = 3) into S5, Y
      *> and Q (GR9c). Crossfooting, in definition order:
      *> S5 = 3 + its own 3 = 6; X (0 so far, RESET ON FINAL) = 0 +
      *> Y 3 + its own 0 = 3; Z = 0 + (Z 0 + Y 3 + Z 0) = 3, Z added
      *> twice as written (GR10); P = Q 3 = 3; Q = 3 + P 3 = 6.
      *> Rolling forward: G5 = 6, GX = 3. Only S5, Y, P and Q reset
      *> (GR12). Footing 2 subtotals 4: S5 = 4 + 4 = 8;
      *> X = 3 + 4 + its own 3 = 10; Z = 3 + (3 + 4 + 3) = 13; P = 4;
      *> Q = 4 + 4 = 8. G5 = 6 + 8 = 14, GX = 3 + 10 = 13.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5895A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PF1 ASSIGN TO "PB5895A.OUT".
       DATA DIVISION.
       FILE SECTION.
       FD  PF1 REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-KEY PIC 9  VALUE 1.
       01  WS-A   PIC 99 VALUE 0.
       REPORT SECTION.
       RD  R1 CONTROLS ARE FINAL WS-KEY.
       01  D1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC 99 SOURCE WS-A.
       01  CFK TYPE CF WS-KEY LINE PLUS 1.
           02  S5 COLUMN 1  PIC 999 SUM WS-A S5.
           02  Y  COLUMN 5  PIC 999 SUM WS-A.
           02  X  COLUMN 9  PIC 999 SUM Y X RESET ON FINAL.
           02  Z  COLUMN 13 PIC 999 SUM Z Y Z RESET ON FINAL.
           02  P  COLUMN 17 PIC 999 SUM Q.
           02  Q  COLUMN 21 PIC 999 SUM WS-A P.
       01  CFF TYPE CF FINAL LINE PLUS 1.
           02  G5 COLUMN 1  PIC 999 SUM S5.
           02  GX COLUMN 5  PIC 999 SUM X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       CFK-USE SECTION.
           USE BEFORE REPORTING CFK.
       CFK-USE-P.
           DISPLAY "CF " S5 " " Y " " X " " Z " " P " " Q.
       CFF-USE SECTION.
           USE BEFORE REPORTING CFF.
       CFF-USE-P.
           DISPLAY "FINAL " G5 " " GX.
       END DECLARATIVES.
       MAIN SECTION.
       P0.
           OPEN OUTPUT PF1.
           INITIATE R1.
           MOVE 1 TO WS-A. GENERATE D1.
           MOVE 2 TO WS-A. GENERATE D1.
           MOVE 2 TO WS-KEY.
           MOVE 4 TO WS-A. GENERATE D1.
           TERMINATE R1.
           CLOSE PF1.
           STOP RUN.
