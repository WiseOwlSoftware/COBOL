      *>--- conformance-case
      *> id: 1985-GR-VI_6_23_4-5
      *> rules: [1985-GR-VI.6.23.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.4 GR5 (THE SET STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETTBL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY IX1 IX2.
       01  XD1 USAGE IS INDEX.
       01  XD2 USAGE IS INDEX.
       01  N PIC 9 VALUE 4.
       01  M PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDE" TO TBL.
           SET IX1 TO 2.
           DISPLAY ELT (IX1).
           SET IX1 TO N.
           DISPLAY ELT (IX1).
           SET M TO IX1.
           DISPLAY M.
           SET IX1 TO 5.
           SET IX2 TO IX1.
           DISPLAY ELT (IX2).
           SET XD1 TO IX2.
           SET XD2 TO XD1.
           SET IX1 TO 1.
           SET IX1 TO XD2.
           DISPLAY ELT (IX1).
           SET IX2 TO 3.
           SET IX2 TO XD1.
           DISPLAY ELT (IX2).
           STOP RUN.
