      *>--- conformance-case
      *> id: g85_f17_02
      *> rules: [1985-GR-VII.3.2.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.2.4 GR1 - a file description entry associates
      *> file-name-1 with a file connector. Two sequential FDs each get
      *> their own connector: records written through SQ-A are read back
      *> through SQ-A, those of SQ-B through SQ-B, and OPEN of one does
      *> not touch the other's status. A shared or crossed connector
      *> shows the other file's record or a wrong count.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1702.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-A ASSIGN TO "G85F17SA.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT SQ-B ASSIGN TO "G85F17SB.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST-B.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-A.
       01  SQ-A-REC PIC X(6).
       FD  SQ-B.
       01  SQ-B-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX.
       01  ST-B PIC XX.
       01  CNT PIC 9 VALUE 0.
       01  EOF PIC X VALUE "N".
       PROCEDURE DIVISION.
       P1.
           MOVE "XX" TO ST-B.
           OPEN OUTPUT SQ-A.
           DISPLAY "OPEN A " ST-A " B " ST-B.
           OPEN OUTPUT SQ-B.
           MOVE "AREC01" TO SQ-A-REC.
           WRITE SQ-A-REC.
           MOVE "AREC02" TO SQ-A-REC.
           WRITE SQ-A-REC.
           MOVE "BREC01" TO SQ-B-REC.
           WRITE SQ-B-REC.
           CLOSE SQ-A SQ-B.
           OPEN INPUT SQ-A SQ-B.
           PERFORM READ-A UNTIL EOF = "Y".
           DISPLAY "A COUNT " CNT.
           MOVE 0 TO CNT.
           MOVE "N" TO EOF.
           PERFORM READ-B UNTIL EOF = "Y".
           DISPLAY "B COUNT " CNT.
           CLOSE SQ-A SQ-B.
           STOP RUN.
       READ-A.
           READ SQ-A AT END MOVE "Y" TO EOF.
           IF EOF = "N"
               ADD 1 TO CNT
               DISPLAY "A " SQ-A-REC.
       READ-B.
           READ SQ-B AT END MOVE "Y" TO EOF.
           IF EOF = "N"
               ADD 1 TO CNT
               DISPLAY "B " SQ-B-REC.
