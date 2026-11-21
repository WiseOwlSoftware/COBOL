*>--- conformance-case
*> id: pb5987-declaratives-section-then-paragraph
*> rules: [FMT-14.2.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5987. A DECLARATIVES portion after a section and followed by a paragraph: COBOL2797 names the ordering
      *> rule (§1985:IV.6.4.1.1, §2023:14.3), not a parser message.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5987SP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5987SP.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           DISPLAY "B".
       DECLARATIVES.
       ERR-S SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-P.
           DISPLAY "X".
       END DECLARATIVES.
       LAST-P.
           STOP RUN.
