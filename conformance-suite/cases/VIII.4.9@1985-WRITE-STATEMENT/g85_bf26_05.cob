      *>--- conformance-case
      *> id: g85_bf26_05
      *> rules: [1985-GR-VIII.4.9.4-9, 1985-GR-VIII.4.9.4-15]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.9.4 GR9 - control after a relative WRITE follows
      *> the INVALID KEY / NOT INVALID KEY phrases (§1985:VIII.1.3.5):
      *> success takes NOT INVALID KEY (W1 OK), the invalid key takes
      *> INVALID KEY and skips NOT INVALID KEY and the USE procedure
      *> (W2 INV), with no INVALID KEY phrase the USE procedure runs
      *> and NOT INVALID KEY is ignored (USE 22, AFTER W3), and with no
      *> phrase a success falls through (AFTER W4 00).
      *> §1985:VIII.4.9.4 GR15 - the WRITE of an existing record number
      *> is unsuccessful: the record area is unaffected (AREA BBB-01),
      *> the I-O status is 22 (§1985:VIII.1.3.4, I-O Status = 22) and
      *> the file keeps the first record (RD1 AAA-01).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2605.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT REL-FILE ASSIGN TO "bf2605.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
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
           MOVE "AAA-01" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "W1 INV " WS-STAT
               NOT INVALID KEY DISPLAY "W1 OK " WS-STAT
           END-WRITE.
           MOVE "BBB-01" TO REL-REC.
           WRITE REL-REC
               INVALID KEY DISPLAY "W2 INV " WS-STAT
               NOT INVALID KEY DISPLAY "W2 OK " WS-STAT
           END-WRITE.
           DISPLAY "AREA " REL-REC.
           WRITE REL-REC
               NOT INVALID KEY DISPLAY "W3 OK " WS-STAT
           END-WRITE.
           DISPLAY "AFTER W3 " WS-STAT.
           MOVE 2 TO WS-KEY.
           MOVE "CCC-02" TO REL-REC.
           WRITE REL-REC.
           DISPLAY "AFTER W4 " WS-STAT.
           CLOSE REL-FILE.
           OPEN INPUT REL-FILE.
           MOVE 1 TO WS-KEY.
           READ REL-FILE.
           DISPLAY "RD1 " REL-REC.
           CLOSE REL-FILE.
           STOP RUN.
