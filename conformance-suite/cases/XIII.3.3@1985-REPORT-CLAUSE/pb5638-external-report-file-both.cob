      *>--- conformance-case
      *> id: pb5638-external-report-file-both
      *> rules: [1985-GR-XIII.3.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5638, the control of the report-file refusal.
      *> §1985:XIII.3.3.4 GR3: every file description entry of an
      *> external file connector "must describe it as a report file".
      *> Both entries here do, each naming its own report (a report-name
      *> is local to its program, so the names may differ): the run unit
      *> translates and runs, and the main program displays DONE after
      *> the called one returns.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5638R2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "p5638r2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL
           REPORT IS RA.
       REPORT SECTION.
       RD  RA
           PAGE LIMIT IS 60 LINES.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P.
           CALL "P5638R2S"
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5638R2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5638R2S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "p5638r2.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL
           REPORT IS RB.
       REPORT SECTION.
       RD  RB
           PAGE LIMIT IS 60 LINES.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "WORLD".
       PROCEDURE DIVISION.
       P.
           EXIT PROGRAM.
       END PROGRAM P5638R2S.
