      *>--- conformance-case
      *> id: 1985-FMT-X_4_3_2
      *> rules: [1985-FMT-X.4.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.4.3.2 (THE DATA DESCRIPTION ENTRY IN THE INTER-PROGRAM
      *> COMMUNICATION MODULE). An output test: the program must
      *> translate and run at 85 and DISPLAY exactly the lines its .out
      *> holds, the outcome the rule requires. Placed from the
      *> confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DDIPC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G-REC IS GLOBAL.
           05  G-A PIC X(3) VALUE "GLB".
           05  G-B PIC X(3) VALUE "REC".
       01  G-CNT IS GLOBAL PICTURE IS 9(3) USAGE IS BINARY
           VALUE IS 7.
       01  E-REC IS EXTERNAL PIC X(5).
       01  L-REC PIC X(5) VALUE "LOCAL".
       01  L-RED REDEFINES L-REC IS GLOBAL PIC X(5).
       01  L-NUM PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "EXTRN" TO E-REC.
           CALL "DDSUB".
           DISPLAY E-REC.
           MOVE G-CNT TO L-NUM.
           DISPLAY L-NUM.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DDSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  E-REC IS EXTERNAL PIC X(5).
       PROCEDURE DIVISION.
       SUB-PARA.
           DISPLAY G-A G-B.
           DISPLAY E-REC.
           DISPLAY L-RED.
           ADD 1 TO G-CNT.
           MOVE "BACK" TO E-REC.
           EXIT PROGRAM.
       END PROGRAM DDSUB.
       END PROGRAM DDIPC.
