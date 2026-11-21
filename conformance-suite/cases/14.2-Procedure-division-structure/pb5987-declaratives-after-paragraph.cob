*>--- conformance-case
*> id: pb5987-declaratives-after-paragraph
*> rules: [FMT-14.2.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5987. A DECLARATIVES portion written after a paragraph of a division without sections is out of
      *> place by the same rule (§1985:IV.6.4.1.1, §2023:14.3): COBOL2797.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5987AP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5987AP.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "B".
           STOP RUN.
       DECLARATIVES.
       ERR-S SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-P.
           DISPLAY "IN-DECLARATIVES".
       END DECLARATIVES.
