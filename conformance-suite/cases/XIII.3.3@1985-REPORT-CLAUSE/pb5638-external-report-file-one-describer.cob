      *>--- conformance-case
      *> id: pb5638-external-report-file-one-describer
      *> rules: [1985-GR-XIII.3.3.4-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5638 (its sweep). §1985:XIII.3.3.4 GR3: "If the
      *> associated file connector is an external file connector, every
      *> file description entry in the run unit which is associated with
      *> that file connector must describe it as a report file." The
      *> first entry writes REPORT IS RR and the second describes a
      *> record instead: refused (COBOL6847). Before the fix this
      *> compiled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5638R1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RF ASSIGN TO "p5638r1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  RF IS EXTERNAL
           REPORT IS RR.
       REPORT SECTION.
       RD  RR
           PAGE LIMIT IS 60 LINES.
       01  DET TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN NUMBER IS 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       P.
           CALL "P5638R1S"
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5638R1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5638R1S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RF ASSIGN TO "p5638r1.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  RF IS EXTERNAL.
       01  RF-REC PIC X(5).
       PROCEDURE DIVISION.
       P.
           EXIT PROGRAM.
       END PROGRAM P5638R1S.
