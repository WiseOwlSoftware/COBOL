      *>--- conformance-case
      *> id: 1985-GR-VI_6_23_4-3
      *> rules: [1985-GR-VI.6.23.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.23.4 GR3 (THE SET STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETTST.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 5 TIMES INDEXED BY IX1.
       01  TBL2.
           05  ELT2 PIC XX OCCURS 5 TIMES INDEXED BY IX2.
       01  NTBL.
           05  NEL PIC 9 OCCURS 5 TIMES.
       01  XD USAGE IS INDEX.
       01  N1 PIC 9 VALUE 1.
       01  N2 PIC 99 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDE" TO TBL.
           MOVE "AABBCCDDEE" TO TBL2.
           MOVE "00000" TO NTBL.
           SET IX1 TO 4.
           DISPLAY ELT (IX1).
           SET IX2 TO IX1.
           DISPLAY ELT2 (IX2).
           SET IX1 TO N2.
           DISPLAY ELT (IX1).
           SET XD TO IX2.
           SET IX2 TO 1.
           DISPLAY ELT2 (IX2).
           SET IX2 TO XD.
           DISPLAY ELT2 (IX2).
           SET IX1 TO 3.
           SET N1 NEL (N1) TO IX1.
           DISPLAY N1.
           DISPLAY NTBL.
           STOP RUN.
