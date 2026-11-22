      *>--- conformance-case
      *> id: 1989-GR-A_2_21_1-p1
      *> rules: [1989-GR-A.2.21.1-p1, 1989-FMT-A.2.21.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Also the accepted witness of §1989:A.2.21.2, the general format
      *> FUNCTION LOG10 ( argument-1 ): each reference below writes that
      *> form (kb/Work PB5740).
      *> §1989:A.2.21.1 p1 (THE LOG10 FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-20.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      *> s-20 probe log10-85: LOG10 value; type numeric (arithmetic use; INTEGER(...) as integer argument is the control)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PLOG10.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(8)V9(6).
       01 I PIC S9(9).
       01 N PIC S9(9).
       01 X PIC X(30).
       01 A1 PIC A(3) VALUE "ABC".
       01 A2 PIC A(3) VALUE "ABD".
       01 OK PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           COMPUTE R ROUNDED = FUNCTION LOG10(1000).
           IF R = 3
               DISPLAY "OK   LOG10 value 1"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL LOG10 value 1"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION LOG10(2).
           IF R = 0.301030
               DISPLAY "OK   LOG10 value 2"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL LOG10 value 2"
           END-IF.
           COMPUTE R ROUNDED = FUNCTION LOG10(1000) * 2 - FUNCTION
               LOG10(1000).
           IF R = 3
               DISPLAY "OK   LOG10 in arithmetic expression"
           ELSE
               MOVE 0 TO OK
               DISPLAY "FAIL LOG10 in arithmetic expression"
           END-IF.
           COMPUTE I = FUNCTION MOD(FUNCTION INTEGER(FUNCTION
               LOG10(1000)) 7).
           IF OK = 1 DISPLAY "PASS" ELSE DISPLAY "FAIL".
           STOP RUN.
