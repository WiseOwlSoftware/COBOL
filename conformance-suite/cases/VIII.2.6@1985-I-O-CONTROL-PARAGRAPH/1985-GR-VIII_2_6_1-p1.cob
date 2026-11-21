      *>--- conformance-case
      *> id: 1985-GR-VIII_2_6_1-p1
      *> rules: [1985-GR-VIII.2.6.1-p1, 1985-GR-VIII.3.2.1-p1, 1985-GR-VIII.4.2.1-p1, 1985-GR-VIII.4.3.1-p1, 1985-GR-VIII.4.4.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.6.1 p1; §1985:VIII.3.2.1 p1; §1985:VIII.4.2.1 p1;
      *> §1985:VIII.4.3.1 p1; §1985:VIII.4.4.1 p1 (THE I-O-CONTROL
      *> PARAGRAPH). An output test: the program must translate and run
      *> at 85 and DISPLAY exactly the lines its .out holds, the outcome
      *> the rule requires. Placed from written and run by the PB5453
      *> reader of lineage-reads-1985/s-14.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * s-14 probe p01: relative file at 85 -- I-O-CONTROL (RERUN
      * and SAME RECORD AREA), the FD, OPEN, DELETE, CLOSE WITH LOCK.
      * Rows: 1985-GR-VIII.2.6.1-p1 VIII.3.2.1-p1 VIII.4.2.1-p1
      *       VIII.4.3.1-p1 VIII.4.4.1-p1.  Expected output in
      *       p01_relative.out (from the 1985 text, not from a run).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P01REL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELA ASSIGN TO "S14RELA"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT RELB ASSIGN TO "S14RELB"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKB
               FILE STATUS IS FSB.
       I-O-CONTROL.
           RERUN ON SYSRR EVERY 100 RECORDS OF RELA
           SAME RECORD AREA FOR RELA RELB.
       DATA DIVISION.
       FILE SECTION.
       FD  RELA
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 8 CHARACTERS
           LABEL RECORDS ARE STANDARD
           DATA RECORD IS RA.
       01  RA              PIC X(8).
       FD  RELB
           LABEL RECORDS ARE STANDARD.
       01  RB              PIC X(8).
       WORKING-STORAGE SECTION.
       01  RK              PIC 9(4).
       01  RKB             PIC 9(4).
       01  FS              PIC XX.
       01  FSB             PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RELA.
           DISPLAY "OPEN-OUT " FS.
           MOVE 1 TO RK. MOVE "REC-0001" TO RA.
           WRITE RA INVALID KEY DISPLAY "BAD W1".
           MOVE 2 TO RK. MOVE "REC-0002" TO RA.
           WRITE RA INVALID KEY DISPLAY "BAD W2".
           CLOSE RELA.
           OPEN I-O RELA.
           DISPLAY "OPEN-IO " FS.
           MOVE 1 TO RK.
           DELETE RELA INVALID KEY DISPLAY "BAD DEL".
           DISPLAY "DELETE " FS.
           MOVE 1 TO RK.
           READ RELA INVALID KEY DISPLAY "GONE " FS.
           MOVE 2 TO RK.
           READ RELA INVALID KEY DISPLAY "BAD R2".
           DISPLAY "KEPT " RA.
           OPEN OUTPUT RELB.
           MOVE 7 TO RKB. MOVE "SHARED-B" TO RB.
           DISPLAY "SAME RA " RA.
           CLOSE RELB.
           CLOSE RELA WITH LOCK.
           DISPLAY "CLOSE-LOCK " FS.
           OPEN INPUT RELA.
           DISPLAY "REOPEN " FS.
           STOP RUN.
