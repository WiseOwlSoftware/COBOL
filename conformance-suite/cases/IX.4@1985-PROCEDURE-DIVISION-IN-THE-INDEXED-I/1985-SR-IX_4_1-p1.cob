      *>--- conformance-case
      *> id: 1985-SR-IX_4_1-p1
      *> rules: [1985-SR-IX.4.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.1 p1 (PROCEDURE DIVISION IN THE INDEXED I-O MODULE).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-14.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-14 probe p03 CONTROL: USE inside DECLARATIVES (accepted).
      * Row 1985-SR-IX.4.1-p1. Pair: p03_use_outside_refused.cob.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P03CTL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "S14IDXC"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS K
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05  K           PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS              PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IX.
       IX-ERR-P.
           DISPLAY "USE " FS.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT IX.
           CLOSE IX.
           OPEN INPUT IX.
           MOVE "0001" TO K.
           READ IX.
           DISPLAY "DONE".
           CLOSE IX.
           STOP RUN.
