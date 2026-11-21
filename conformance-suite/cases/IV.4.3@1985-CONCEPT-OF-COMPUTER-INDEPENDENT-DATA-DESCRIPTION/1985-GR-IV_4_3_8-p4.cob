      *>--- conformance-case
      *> id: 1985-GR-IV_4_3_8-p4
      *> rules: [1985-GR-IV.4.3.8-p4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8 p4 (CONCEPT OF COMPUTER INDEPENDENT DATA
      *> DESCRIPTION). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-05.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.4.3.8-p4: subscripting and reference modification
      * are evaluated once as the first operation of the statement
      * (STRING has no rule of its own). PTR redefines B, which the
      * STRING changes while it runs.
      * 1) C (PTR) stays C (1): C1=0004, C123=0000.
      * 2) delimiter D (PTR:1) stays D (1:1) = "X": B becomes 121; a
      *    re-evaluated D (101:1) = "2" would stop it at 101.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S05ONCE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A           PIC X(10) VALUE "123 ".
       01  A2          PIC X(10) VALUE "12X3".
       01  B           PIC XXX.
       01  PTR REDEFINES B PIC 999.
       01  D           PIC X(200).
       01  CC.
           02  C       PIC 9(4) OCCURS 200 TIMES.
       PROCEDURE DIVISION.
       P1.
           MOVE ZERO TO CC.
           MOVE 1 TO PTR.
           MOVE 1 TO C (1).
           STRING A DELIMITED BY SPACE INTO B WITH POINTER C (PTR)
               ON OVERFLOW DISPLAY "OVERFLOW"
           END-STRING.
           DISPLAY "B=" B.
           DISPLAY "C1=" C (1).
           DISPLAY "C123=" C (123).
           MOVE ALL "2" TO D.
           MOVE "X" TO D (1:1).
           MOVE 1 TO PTR.
           STRING A2 DELIMITED BY D (PTR:1) INTO B.
           DISPLAY "B2=" B.
           STOP RUN.
