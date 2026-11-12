      *>--- conformance-case
      *> id: 1985-SR-XI_2_3_3-2
      *> rules: [1985-SR-XI.2.3.3-2]
      *> source-format: fixed
      *>---
      *> §1985:XI.2.3.3 SR2 (THE FILE CONTROL ENTRY, sort-merge):
      *> "Since file-name-1 represents a sort or merge file, only the
      *> ASSIGN clause is permitted to follow file-name-1 in the
      *> FILE-CONTROL paragraph." SRT is described by an SD and its
      *> file control entry adds ORGANIZATION IS SEQUENTIAL, which
      *> the 2002 and later Format 4 prints and the 1985 format
      *> (§1985:XI.2.3.2, SELECT file-name-1 ASSIGN TO ...) does not.
      *> A refusal at 85: a compiler that accepts it fails
      *> (kb/Work PB5071).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5071SD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SRT ASSIGN TO "N5071SD"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD  SRT.
       01  SRT-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
