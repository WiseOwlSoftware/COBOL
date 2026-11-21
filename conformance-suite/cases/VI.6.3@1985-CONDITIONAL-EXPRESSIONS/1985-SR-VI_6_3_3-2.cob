      *>--- conformance-case
      *> id: 1985-SR-VI_6_3_3-2
      *> rules: [1985-SR-VI.6.3.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.3 SR2 (CONDITIONAL EXPRESSIONS). A refusal: the
      *> rule forbids line 10 of the program below, and WiseOwl refuses
      *> it at 85 with COBOL0900 there; a compiler that accepts it
      *> fails. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. XOR85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       01 C PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       MAIN.
           IF A = B XOR C
               DISPLAY "XOR-ACCEPTED"
           ELSE
               DISPLAY "XOR-ACCEPTED-FALSE"
           END-IF
           STOP RUN.
