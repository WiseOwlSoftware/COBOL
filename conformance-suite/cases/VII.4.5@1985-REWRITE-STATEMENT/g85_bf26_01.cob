      *>--- conformance-case
      *> id: g85_bf26_01
      *> rules: [1985-GR-VII.4.5.4-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.5.4 GR7 - every execution of a sequential REWRITE
      *> updates the file's I-O status: an unsuccessful one with no
      *> preceding READ sets 43 (§1985:VII.1.3.5, "was not a
      *> successfully executed READ statement"), a successful one
      *> overwrites a planted 99 with 00, and one on a file open INPUT
      *> sets 49 (§1985:VII.1.3.5, "not open in the I-O mode"). A
      *> leg fails if the status is left as it was or set wrongly;
      *> the reread fails if the unsuccessful REWRITE replaced data.
      *> After a critical 4x status WiseOwl COBOL continues with the
      *> next statement (docs/conformance/3.5, kb/Work PB5591).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2601.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "bf2601.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE.
       01  SQ-REC PIC X(6).
       WORKING-STORAGE SECTION.
       01  SQ-STAT PIC XX VALUE "??".
       PROCEDURE DIVISION.
       DECLARATIVES.
       SQ-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SQ-FILE.
       SQ-ERR-1.
           DISPLAY "USE " SQ-STAT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT SQ-FILE.
           MOVE "AAAAAA" TO SQ-REC.
           WRITE SQ-REC.
           MOVE "BBBBBB" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ-FILE.
           OPEN I-O SQ-FILE.
      *    Leg 1: no READ precedes the REWRITE.
           MOVE "XXXXXX" TO SQ-REC.
           REWRITE SQ-REC.
           DISPLAY "R1 " SQ-STAT.
      *    Leg 2: a successful READ precedes it; 99 is planted.
           READ SQ-FILE.
           MOVE "CCCCCC" TO SQ-REC.
           MOVE "99" TO SQ-STAT.
           REWRITE SQ-REC.
           DISPLAY "R2 " SQ-STAT.
           CLOSE SQ-FILE.
      *    Leg 3: the file is open in the input mode.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE.
           REWRITE SQ-REC.
           DISPLAY "R3 " SQ-STAT.
           CLOSE SQ-FILE.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE.
           DISPLAY "REC1 " SQ-REC.
           READ SQ-FILE.
           DISPLAY "REC2 " SQ-REC.
           CLOSE SQ-FILE.
           STOP RUN.
