      *>--- conformance-case
      *> id: g85_bf26_04
      *> rules: [1985-GR-VIII.4.7.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.7.4 GR1 - START requires the relative file open
      *> in the input or I-O mode. Open INPUT (S-IN) and open I-O
      *> (S-IO) it succeeds with 00 and positions the file for READ
      *> NEXT; open OUTPUT (S-OUT) and not open (S-CLOSED) it fails
      *> with 47 (§1985:VIII.1.3.4, I-O Status = 47, "READ or START
      *> statement is attempted on a file not open in the input or
      *> I-O mode") and the USE procedure runs. WiseOwl COBOL
      *> continues after the critical status (docs/conformance/3.5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2604.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "bf2604.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS WS-KEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE.
       01  REL-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  WS-KEY  PIC 9(4).
       01  WS-STAT PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       REL-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON REL-FILE.
       REL-ERR-1.
           DISPLAY "USE " WS-STAT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           START REL-FILE KEY IS EQUAL TO WS-KEY.
           DISPLAY "S-OUT " WS-STAT.
           MOVE 1 TO WS-KEY.
           MOVE "REC-01" TO REL-REC.
           WRITE REL-REC.
           MOVE 2 TO WS-KEY.
           MOVE "REC-02" TO REL-REC.
           WRITE REL-REC.
           MOVE 3 TO WS-KEY.
           MOVE "REC-03" TO REL-REC.
           WRITE REL-REC.
           CLOSE REL-FILE.
           MOVE 1 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           START REL-FILE KEY IS EQUAL TO WS-KEY.
           DISPLAY "S-CLOSED " WS-STAT.
           OPEN INPUT REL-FILE.
           MOVE 2 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           START REL-FILE KEY IS NOT LESS THAN WS-KEY.
           DISPLAY "S-IN " WS-STAT.
           READ REL-FILE NEXT RECORD.
           DISPLAY "NEXT " REL-REC.
           CLOSE REL-FILE.
           OPEN I-O REL-FILE.
           MOVE 3 TO WS-KEY.
           MOVE "99" TO WS-STAT.
           START REL-FILE KEY IS EQUAL TO WS-KEY.
           DISPLAY "S-IO " WS-STAT.
           READ REL-FILE NEXT RECORD.
           DISPLAY "NEXT " REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
