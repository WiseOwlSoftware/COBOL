      *>--- conformance-case
      *> id: pb5987-declaratives-first-legal
      *> rules: [FMT-14.2.1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5987. The legal side of the ordering rule (§1985:IV.6.4.1.1, §2023:14.3): the DECLARATIVES portion
      *> first, then the nondeclarative sections, whose paragraphs run in order; the declarative never runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5987OK.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5987OK.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-S SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-P.
           DISPLAY "IN-DECLARATIVES".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           DISPLAY "MAIN-P".
       SUB-P.
           DISPLAY "SUB-P".
       NEXT-S SECTION.
       NEXT-P.
           DISPLAY "NEXT-S".
           STOP RUN.
