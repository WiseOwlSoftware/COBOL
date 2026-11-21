      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-p1
      *> rules: [1985-GR-IV.6.4.2.1.1-p1, 1985-GR-IV.6.4.2.1.1.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 p1; §1985:IV.6.4.2.1.1.1 p1 (Conditional
      *> Statements and Sentences). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from written and
      *> run by the PB5453 reader of lineage-reads-1985/s-06.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * 1985-GR-IV.6.4.2.1.1-p1 / -IV.6.4.2.1.1.1-p1: a conditional
      * statement determines a truth value and the subsequent action
      * depends on it; a conditional phrase names the action taken on
      * that truth value.
      * Expect: IF-T / IF-F / SZ-ERR 99 / SZ-OK 50 / EOF / COND OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONDST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "S06CND.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC  PIC X(4).
       WORKING-STORAGE SECTION.
       77  N   PIC 99 VALUE 99.
       77  K   PIC 99 VALUE 1.
       PROCEDURE DIVISION.
       P0.
           IF K = 1 DISPLAY "IF-T" ELSE DISPLAY "IF-X".
           IF K = 2 DISPLAY "IF-X" ELSE DISPLAY "IF-F".
           ADD 1 TO N ON SIZE ERROR DISPLAY "SZ-ERR " N
               NOT ON SIZE ERROR DISPLAY "SZ-NOT " N.
           MOVE 49 TO N.
           ADD 1 TO N ON SIZE ERROR DISPLAY "SZ-ERR " N
               NOT ON SIZE ERROR DISPLAY "SZ-OK " N.
           OPEN OUTPUT F1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF"
               NOT AT END DISPLAY "REC".
           CLOSE F1.
           DISPLAY "COND OK".
           STOP RUN.
