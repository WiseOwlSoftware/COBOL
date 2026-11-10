      *>--- conformance-case
      *> id: pb5080-contained-report-local-fd-2002
      *> rules: [SR-14.9.21.3-2, SR-14.9.46.3-2, SR-14.9.16.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.20.2 SR2 (INITIATE), §2002:14.8.42.2 SR2 (TERMINATE) and §2002:14.8.15.2 SR4 (GENERATE),
      *> restated at §2023:14.9.21.3 SR2, §2023:14.9.46.3 SR2 and §2023:14.9.16.3 SR4: "If report-name-1 is defined
      *> in a containing program, the file description entry associated with report-name-1 shall contain a GLOBAL
      *> clause." RPT1 is GLOBAL but FD RPT-FILE is not, so the contained program's three statements are refused
      *> from 2002 on (COBOL2392). At 85 the same program is accepted: conformance:cases/14.9.21-INITIATE/
      *> pb5080-contained-report-local-fd-85 (kb/Work PB5080).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5080C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "pb5080c.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT1.
       REPORT SECTION.
       RD  RPT1 IS GLOBAL
           PAGE LIMIT IS 10 LINES FIRST DETAIL 3.
       01  DET-LINE TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC X(4) VALUE "BODY".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RPT-FILE.
           CALL "PB5080D".
           CLOSE RPT-FILE.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5080D.
       PROCEDURE DIVISION.
       INNER-PARA.
           INITIATE RPT1.
           GENERATE DET-LINE.
           TERMINATE RPT1.
           EXIT PROGRAM.
       END PROGRAM PB5080D.
       END PROGRAM PB5080C.
