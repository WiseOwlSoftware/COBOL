      *>--- conformance-case
      *> id: 1985-GR-IX_2_3_4-1
      *> rules: [1985-GR-IX.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR1 (THE FILE CONTROL ENTRY). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MAINPGM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL IXF ASSIGN TO "IXFDATA"
               RESERVE 2 AREAS
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXF-KEY
               ALTERNATE RECORD KEY IS IXF-ALT WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF IS EXTERNAL.
       01  IXF-REC.
           05 IXF-KEY  PIC X(4).
           05 IXF-ALT  PIC X(2).
           05 IXF-DATA PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IXF.
           MOVE "K001" TO IXF-KEY.
           MOVE "A1" TO IXF-ALT.
           MOVE "ALPHA" TO IXF-DATA.
           WRITE IXF-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IXF.
           OPEN INPUT IXF.
           CALL "SUBPGM".
           CLOSE IXF.
           DISPLAY "MAIN DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBPGM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL IXF ASSIGN TO "IXFDATA"
               RESERVE 2 AREAS
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXF-KEY
               ALTERNATE RECORD KEY IS IXF-ALT WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF IS EXTERNAL.
       01  IXF-REC.
           05 IXF-KEY  PIC X(4).
           05 IXF-ALT  PIC X(2).
           05 IXF-DATA PIC X(5).
       PROCEDURE DIVISION.
       SUB-PARA.
           MOVE "K001" TO IXF-KEY.
           READ IXF RECORD INVALID KEY DISPLAY "READ FAILED".
           DISPLAY IXF-DATA.
           MOVE "A1" TO IXF-ALT.
           READ IXF RECORD KEY IS IXF-ALT
               INVALID KEY DISPLAY "ALT READ FAILED".
           DISPLAY IXF-KEY.
           EXIT PROGRAM.
       END PROGRAM SUBPGM.
       END PROGRAM MAINPGM.
