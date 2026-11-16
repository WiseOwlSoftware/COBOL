      *>--- conformance-case
      *> id: pb5782_numeric_control_signed_zero
      *> rules: [GR-13.18.16.4-3, 1985-GR-XIII.3.7.4-3]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5782 - a numeric CONTROL item is compared as two numeric operands. §1985:XIII.3.7.4 GR3 (cite.py: OK):
      *> "If the control data item is a numeric data item, the relation test is for the comparison of two numeric operands";
      *> §2023:8.8.4.2.4 (cite.py: OK): "Zero is considered a unique value regardless of the sign; an operand having the
      *> value zero is equal to any other operand having the value zero, whether either operand is positive, negative, or
      *> unsigned"; §2023:13.18.16.4 GR3 (cite.py: OK): a break is tested against a prior control "having the same data
      *> description as the corresponding data item".
      *> DERIVATION. K1 is a zoned item with a separate sign and K2 a packed one, each holding +0 at the first GENERATE (the
      *> save). Between the first and second GENERATE K1 becomes -0 ("-0") and K2 becomes -0 (the one byte 0D, FUNCTION CHAR(14)): each equals zero
      *> (the IF lines show it), so the second GENERATE breaks nothing. Then K2 becomes 1: that is a change, the break is at
      *> K2 and CFK2 prints once during that GENERATE. TERMINATE prints CFK2 then CFK1.
      *>   expected: ZERO1 ZERO2 / CFK2 (the change to 1) / CFK2 / CFK1 (TERMINATE).
      *> Each leg can fail: an image compare of -0 against +0 prints CFK2 and CFK1 before the K2 change; a compare that never
      *> breaks omits the CFK2 of the change.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5782T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5782T.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  G1.
           05  K1    PIC S9 SIGN LEADING SEPARATE VALUE +0.
           05  K1-X  REDEFINES K1 PIC XX.
       01  G2.
           05  K2    PIC S9 COMP-3 VALUE 0.
           05  K2-X  REDEFINES K2 PIC X.
       REPORT SECTION.
       RD  RPT CONTROLS ARE K1 K2.
       01  DTL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "DTL".
       01  CFK2 TYPE IS CONTROL FOOTING K2 LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "CFK2".
       01  CFK1 TYPE IS CONTROL FOOTING K1 LINE PLUS 1.
           05  COLUMN 1 PIC X(4) VALUE "CFK1".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-K2 SECTION.
           USE BEFORE REPORTING CFK2.
       U-K2-P.
           DISPLAY "CFK2".
       U-K1 SECTION.
           USE BEFORE REPORTING CFK1.
       U-K1-P.
           DISPLAY "CFK1".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           GENERATE DTL.
           MOVE "-0" TO K1-X.
           MOVE FUNCTION CHAR(14) TO K2-X.
           IF K1 = 0 DISPLAY "ZERO1".
           IF K2 = 0 DISPLAY "ZERO2".
           GENERATE DTL.
           MOVE 1 TO K2.
           GENERATE DTL.
           TERMINATE RPT.
           CLOSE PRT.
           STOP RUN.
