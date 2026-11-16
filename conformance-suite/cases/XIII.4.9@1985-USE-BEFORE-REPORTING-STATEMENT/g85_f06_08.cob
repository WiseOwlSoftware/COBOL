      *>--- conformance-case
      *> id: g85_f06_08
      *> rules: [1985-GR-XIII.4.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.9.4 GR1 - USE BEFORE REPORTING declaratives in
      *>   a containing program (G85F0608) AND in the program it
      *>   contains (G85F0608I), each for its own report; each
      *>   declarative is invoked just before its detail group is
      *>   produced (§1985:XIII.3.20.4 GR11: subtotalling, then the USE
      *>   procedure, then formatting and presentation).
      *> The proof of "just before": the declarative reads the
      *>   report's LINE-COUNTER before the group is presented, the
      *>   main line reads it after. PAGE LIMIT 20 LINES, FIRST DETAIL
      *>   3, LAST DETAIL 15; DET is LINE PLUS 1 with no NEXT GROUP
      *>   (§1985:XIII.3.10.8 Table 3 row "R": GR3b, GR4b, GR6d):
      *>   INITIATE sets LINE-COUNTER to 0 (§1985:XIII.3.5.6 GR4); the
      *>   first DET goes on FIRST DETAIL line 3 (4b, 0 < 3), the
      *>   second on 3 + 1 = 4; after each the setting is that line
      *>   (6d). A declarative run after presentation would read 03 and
      *>   04, one never run prints no USE line, and one in the
      *>   contained program ignored prints no "I USE" line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0608.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT O-RPT-FILE ASSIGN TO G85F08O.
       DATA DIVISION.
       FILE SECTION.
       FD  O-RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS O-RPT.
       WORKING-STORAGE SECTION.
       01  WS-LC PIC 9(2).
       01  WS-V  PIC X(4) VALUE "MAIN".
       REPORT SECTION.
       RD  O-RPT
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 15.
       01  O-DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC X(4) SOURCE WS-V.
       PROCEDURE DIVISION.
       DECLARATIVES.
       O-USE SECTION.
           USE BEFORE REPORTING O-DET.
       O-USE-P.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "O USE LC=" WS-LC.
           MOVE "DECL" TO WS-V.
       END DECLARATIVES.
       O-MAIN SECTION.
       O-MAIN-P.
           OPEN OUTPUT O-RPT-FILE.
           INITIATE O-RPT.
           GENERATE O-DET.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "O AFTER LC=" WS-LC.
           GENERATE O-DET.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "O AFTER LC=" WS-LC.
           TERMINATE O-RPT.
           CLOSE O-RPT-FILE.
           CALL "G85F0608I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0608I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT I-RPT-FILE ASSIGN TO G85F08I.
       DATA DIVISION.
       FILE SECTION.
       FD  I-RPT-FILE
           LABEL RECORDS ARE STANDARD
           REPORT IS I-RPT.
       WORKING-STORAGE SECTION.
       01  WS-ILC PIC 9(2).
       01  WS-IV  PIC X(4) VALUE "INNR".
       REPORT SECTION.
       RD  I-RPT
           PAGE LIMIT IS 20 LINES
           HEADING 1
           FIRST DETAIL 3
           LAST DETAIL 15.
       01  I-DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC X(4) SOURCE WS-IV.
       PROCEDURE DIVISION.
       DECLARATIVES.
       I-USE SECTION.
           USE BEFORE REPORTING I-DET.
       I-USE-P.
           MOVE LINE-COUNTER TO WS-ILC.
           DISPLAY "I USE LC=" WS-ILC.
       END DECLARATIVES.
       I-MAIN SECTION.
       I-MAIN-P.
           OPEN OUTPUT I-RPT-FILE.
           INITIATE I-RPT.
           GENERATE I-DET.
           MOVE LINE-COUNTER TO WS-ILC.
           DISPLAY "I AFTER LC=" WS-ILC.
           TERMINATE I-RPT.
           CLOSE I-RPT-FILE.
           EXIT PROGRAM.
       END PROGRAM G85F0608I.
       END PROGRAM G85F0608.
