      *>--- conformance-case
      *> id: g85_f17_05
      *> rules: [1985-GR-VIII.3.2.4-1, 1985-GR-VIII.3.2.4-2, 1985-GR-VIII.3.2.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Two relative files. §1985:VIII.3.2.4 GR1 - each FD associates its
      *> file-name with its own connector: line READ RA 1 shows RA's record
      *> after RB wrote its own record 1. §1985:VIII.3.2.4 GR2 - the
      *> Sequential I-O BLOCK CONTAINS clause: RA states 2 RECORDS,
      *> RB states 20 TO 400 CHARACTERS (§1985:VII.3.3.3 GR4: minimum and
      *> maximum). §1985:VIII.3.2.4 GR5 - the Sequential I-O RECORD clause:
      *> RA format 1 (CONTAINS 10 CHARACTERS), RB format 2 (VARYING IN SIZE
      *> FROM 4 TO 12 DEPENDING ON RB-LEN): after each READ RB-LEN holds the
      *> length of the record read (§1985:VII.3.8.4 GR11), 12 and 04.
      *> RB-LEN is set to 99 before each READ, so a length not stored
      *> shows 99; a refused clause fails translation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1705.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RA ASSIGN TO "G85F17RA.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RA-KEY.
           SELECT RB ASSIGN TO "G85F17RB.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RB-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  RA
           BLOCK CONTAINS 2 RECORDS
           RECORD CONTAINS 10 CHARACTERS.
       01  RA-REC PIC X(10).
       FD  RB
           BLOCK CONTAINS 20 TO 400 CHARACTERS
           RECORD IS VARYING IN SIZE FROM 4 TO 12 CHARACTERS
               DEPENDING ON RB-LEN.
       01  RB-SHORT PIC X(4).
       01  RB-LONG PIC X(12).
       WORKING-STORAGE SECTION.
       01  RA-KEY PIC 9(4).
       01  RB-KEY PIC 9(4).
       01  RB-LEN PIC 99.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RA RB.
           MOVE 1 TO RA-KEY.
           MOVE "RA-RECORD1" TO RA-REC.
           WRITE RA-REC INVALID KEY DISPLAY "RA WRITE FAILED".
           MOVE 1 TO RB-KEY.
           MOVE 4 TO RB-LEN.
           MOVE "RB-S" TO RB-SHORT.
           WRITE RB-SHORT INVALID KEY DISPLAY "RB WRITE 1 FAILED".
           MOVE 2 TO RB-KEY.
           MOVE 12 TO RB-LEN.
           MOVE "RB-LONG-REC2" TO RB-LONG.
           WRITE RB-LONG INVALID KEY DISPLAY "RB WRITE 2 FAILED".
           CLOSE RA RB.
           OPEN INPUT RA RB.
           MOVE 1 TO RA-KEY.
           READ RA INVALID KEY DISPLAY "RA READ FAILED".
           DISPLAY "READ RA 1 " RA-REC.
           MOVE 2 TO RB-KEY.
           MOVE 99 TO RB-LEN.
           READ RB INVALID KEY DISPLAY "RB READ 2 FAILED".
           DISPLAY "READ RB 2 " RB-LONG " LEN " RB-LEN.
           MOVE 1 TO RB-KEY.
           MOVE 99 TO RB-LEN.
           READ RB INVALID KEY DISPLAY "RB READ 1 FAILED".
           DISPLAY "READ RB 1 " RB-SHORT " LEN " RB-LEN.
           CLOSE RA RB.
           STOP RUN.
