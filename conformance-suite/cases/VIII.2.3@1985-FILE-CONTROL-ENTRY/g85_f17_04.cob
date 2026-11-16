      *>--- conformance-case
      *> id: g85_f17_04
      *> rules: [1985-GR-VIII.2.3.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.3.4 GR6 - the FILE STATUS clause of a relative file
      *> is the Sequential I-O clause, its content the relative I-O status
      *> (§1985:VIII.1.3.4). Each statement's value: OPEN 00, WRITE 00,
      *> a WRITE of an existing relative record number 22, READ of an
      *> absent record 23, READ of a present one 00, CLOSE 00. Each status
      *> item is set to XX first, so an item left unchanged shows.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1704.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-F ASSIGN TO "G85F17RS.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-F.
       01  RL-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4).
       01  ST PIC XX.
       PROCEDURE DIVISION.
       P1.
           MOVE "XX" TO ST.
           OPEN OUTPUT REL-F.
           DISPLAY "OPEN OUTPUT " ST.
           MOVE 1 TO RK.
           MOVE "REC001" TO RL-REC.
           MOVE "XX" TO ST.
           WRITE RL-REC INVALID KEY DISPLAY "INVALID 1".
           DISPLAY "WRITE 1 " ST.
           MOVE "REC999" TO RL-REC.
           MOVE "XX" TO ST.
           WRITE RL-REC INVALID KEY DISPLAY "INVALID DUP".
           DISPLAY "WRITE DUP " ST.
           MOVE "XX" TO ST.
           CLOSE REL-F.
           DISPLAY "CLOSE " ST.
           MOVE "XX" TO ST.
           OPEN INPUT REL-F.
           DISPLAY "OPEN INPUT " ST.
           MOVE 5 TO RK.
           MOVE "XX" TO ST.
           READ REL-F INVALID KEY DISPLAY "INVALID 5".
           DISPLAY "READ 5 " ST.
           MOVE 1 TO RK.
           MOVE "XX" TO ST.
           READ REL-F INVALID KEY DISPLAY "INVALID 1".
           DISPLAY "READ 1 " ST " " RL-REC.
           CLOSE REL-F.
           STOP RUN.
