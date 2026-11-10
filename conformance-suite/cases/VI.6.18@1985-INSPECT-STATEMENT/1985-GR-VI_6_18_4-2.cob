      *>--- conformance-case
      *> id: 1985-GR-VI_6_18_4-2
      *> rules: [1985-GR-VI.6.18.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.18.4 GR2 (THE INSPECT STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. INSPLR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  DX  PIC X(5).
       01  DA  PIC A(5).
       01  DN  PIC 9(5).
       01  C1  PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "AAAAA" TO DX.
           INSPECT DX REPLACING ALL "AA" BY "XY".
           DISPLAY "PAIRS " DX.
           MOVE "ABABA" TO DX.
           INSPECT DX REPLACING FIRST "A" BY "Z".
           DISPLAY "FIRST " DX.
           MOVE "BBABB" TO DX.
           INSPECT DX REPLACING LEADING "B" BY "Z".
           DISPLAY "LEADING " DX.
           MOVE "AXAXA" TO DX.
           MOVE ZERO TO C1.
           INSPECT DX TALLYING C1 FOR ALL "A" BEFORE INITIAL "X".
           DISPLAY "BEFORE " C1.
           MOVE ZERO TO C1.
           INSPECT DX TALLYING C1 FOR ALL "A" AFTER INITIAL "X".
           DISPLAY "AFTER " C1.
           MOVE "QQQRQ" TO DA.
           INSPECT DA REPLACING ALL "QQ" BY "ST" FIRST "Q" BY "U".
           DISPLAY "ALPHABETIC " DA.
           MOVE 11101 TO DN.
           INSPECT DN REPLACING ALL "11" BY "23" FIRST "1" BY "9".
           DISPLAY "NUMERIC " DN.
           MOVE 00120 TO DN.
           MOVE ZERO TO C1.
           INSPECT DN TALLYING C1 FOR LEADING "0".
           DISPLAY "ZEROS " C1.
           STOP RUN.
