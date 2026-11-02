*>--- conformance-case
*> id: pb4306-screen-section-constant-entry
*> rules: [FMT-13.9.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4306 - ISO 13.9.2 prints the screen section as SCREEN SECTION. followed by any number of constant-entry or
*> screen-description-entry, so 01 K CONSTANT AS 5. is a conforming entry of the section. This compiler declines the
*> screen facility (Annex A.4.2, docs/CONFORMANCE.md section 5), so the section is refused by name, COBOL1560, and the
*> constant is a declared constant-name (13.10) that MOVE K TO W references without an undefined-reference verdict.
*> Fails if the constant entry draws a syntax error (COBOL0001) in place of the section's named refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4306S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       SCREEN SECTION.
       01 K CONSTANT AS 5.
       01 SCR.
          05 LINE 1 COLUMN 1 VALUE "X".
       PROCEDURE DIVISION.
           MOVE K TO W
           DISPLAY W
           STOP RUN.
