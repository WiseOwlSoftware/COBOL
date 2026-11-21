*>--- conformance-case
*> id: pb5987-declaratives-after-section
*> rules: [FMT-14.2.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5987. §1985:IV.6.4.1.1: "Declarative sections must be grouped at the beginning of the Procedure
      *> Division" (§2023:14.3 the same): a DECLARATIVES portion written after a nondeclarative section is refused by
      *> that ordering rule (COBOL2797), never as a reserved word used as a user-defined word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5987AS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5987AS.TMP".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-S SECTION.
       MAIN-P.
           DISPLAY "B".
           STOP RUN.
       DECLARATIVES.
       ERR-S SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       ERR-P.
           DISPLAY "IN-DECLARATIVES".
       END DECLARATIVES.
