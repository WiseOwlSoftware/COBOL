      *>--- conformance-case
      *> id: g85_f02_10
      *> rules: [1985-GR-X.5.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.3.4 GR4 - CANCEL "G85F02SL" also cancels its
      *> contained G85F02KL, as if a CANCEL named KL, so the implicit
      *> CLOSE of §1985:X.5.3.4 GR10 runs for KL's open file WF at the
      *> CANCEL, before any further CALL: main reads the record KL
      *> wrote (MAIN KKKK, MAIN EOF), and the next OPEN OUTPUT in KL
      *> finds the file closed (A 00, not 41).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0210.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT INFL ASSIGN TO "G85F0210.DAT"
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  INFL.
       01  RR PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS  PIC XX.
       PROCEDURE DIVISION.
       P-MAIN.
           CALL "G85F02SL".
           CANCEL "G85F02SL".
           OPEN INPUT INFL.
           DISPLAY "MAIN OPEN " FS.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           READ INFL AT END MOVE "EOF " TO RR.
           DISPLAY "MAIN " RR.
           CLOSE INFL.
           CALL "G85F02SL".
           STOP RUN.
       END PROGRAM G85F0210.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02SL.
       PROCEDURE DIVISION.
       P-SL.
           CALL "G85F02KL".
           EXIT PROGRAM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F02KL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT WF ASSIGN TO "G85F0210.DAT"
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD  WF.
       01  WR PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-FS PIC XX.
       PROCEDURE DIVISION.
       P-KL.
           OPEN OUTPUT WF.
           DISPLAY "A " WS-FS.
           MOVE "KKKK" TO WR.
           WRITE WR.
           EXIT PROGRAM.
       END PROGRAM G85F02KL.
       END PROGRAM G85F02SL.
