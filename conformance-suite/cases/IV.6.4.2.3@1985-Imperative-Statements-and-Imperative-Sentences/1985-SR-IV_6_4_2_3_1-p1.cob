      *>--- conformance-case
      *> id: 1985-SR-IV_6_4_2_3_1-p1
      *> rules: [1985-SR-IV.6.4.2.3.1-p1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.3.1 p1 (Imperative Statements and Imperative
      *> Sentences). A refusal: the rule forbids line 16 of the program
      *> below, and WiseOwl refuses it at 85 with COBOL2796 there; a
      *> compiler that accepts it fails. Placed from written and run by
      *> the PB5453 reader of lineage-reads-1985/s-07.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P02CI.
      * 1985 IV.6.4.2.3.1: a conditional statement (a verb with its
      * conditional phrase, no scope terminator) is not an imperative
      * statement, so it may not stand where the format writes
      * imperative-statement (ON SIZE ERROR of ADD). Expect refusal.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A    PIC 9 VALUE 0.
       01 N    PIC 9 VALUE 9.
       01 S1   PIC X(4) VALUE "ABCD".
       01 S2   PIC X(2) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD 1 TO N ON SIZE ERROR
               IF A = 1 DISPLAY "X".
           DISPLAY "END".
           STOP RUN.
