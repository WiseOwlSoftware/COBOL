      *>--- conformance-case
      *> id: g85_f29_01
      *> rules: [1985-SR-XIII.3.7.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.7.3 SR1 - "Data-name-1 must not be defined in
      *> the Report Section. Data-name-1 may be qualified." The rule
      *> grants a form: a control data item defined OUTSIDE the
      *> Report Section (here one in the File Section, one in the
      *> Working-Storage Section), each named in the CONTROL clause
      *> with a qualifier (OF a record-name, IN a group).
      *> Why each leg can fail: a compiler that refuses a qualified
      *> data-name-1, or one in the File Section, rejects legal
      *> source; one that resolves the qualified names to the wrong
      *> items, or drops a level, prints the wrong footing sequence.
      *> Derivation (§1985:XIII.3.7.4 GR1-GR2, §1985:XIII.4.3.4 GR6
      *> and §1985:XIII.3.20.4 GR10): F-KEY is the major control,
      *> W-KEY the minor. The USE procedure of each footing shows
      *> which footing ran and the sum counter before its reset.
      *>   GENERATE A/P amt 1, A/P amt 2 : no break      TW = 3
      *>   GENERATE A/Q amt 4 : minor break   -> "CFW 03", TW = 4
      *>   GENERATE B/Q amt 8 : major break   -> "CFW 04" then "CFF"
      *>   TERMINATE           : all footings -> "CFW 08" then "CFF"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2901.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-F ASSIGN TO "g85f2901.txt".
           SELECT WRK-F ASSIGN TO "g85f2901.wrk".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-F REPORT IS RPT.
       FD  WRK-F.
       01  WRK-REC.
           02  F-KEY       PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A.
           02  W-KEY       PIC X  VALUE "P".
           02  W-AMT       PIC 99 VALUE 0.
       01  WS-N            PIC 99 VALUE 0.
       REPORT SECTION.
       RD  RPT CONTROLS ARE F-KEY OF WRK-REC W-KEY IN WS-A.
       01  DE-G TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X  SOURCE F-KEY.
           02  COLUMN 3 PIC X  SOURCE W-KEY.
           02  COLUMN 5 PIC 99 SOURCE W-AMT.
       01  CF-W TYPE CF W-KEY LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFW".
           02  TW COLUMN 5 PIC 99 SUM W-AMT.
       01  CF-F TYPE CF F-KEY LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "CFF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-CFW SECTION.
           USE BEFORE REPORTING CF-W.
       D-CFW-P.
           MOVE TW TO WS-N.
           DISPLAY "CFW " WS-N.
       D-CFF SECTION.
           USE BEFORE REPORTING CF-F.
       D-CFF-P.
           DISPLAY "CFF".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT RPT-F WRK-F.
           MOVE "A" TO F-KEY.
           INITIATE RPT.
           MOVE 1 TO W-AMT.
           GENERATE DE-G.
           MOVE 2 TO W-AMT.
           GENERATE DE-G.
           MOVE "Q" TO W-KEY.
           MOVE 4 TO W-AMT.
           GENERATE DE-G.
           MOVE "B" TO F-KEY.
           MOVE 8 TO W-AMT.
           GENERATE DE-G.
           TERMINATE RPT.
           CLOSE RPT-F WRK-F.
           STOP RUN.
