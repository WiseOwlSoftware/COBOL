      *>--- conformance-case
      *> id: pb5790-report-sign-separate-blank-when-zero-85
      *> rules: [1985-SR-XIII.3.17.3-1, 1985-SR-XIII.3.9.3-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> output-file: {name: pb5790r.txt, expected: pb5790-report-sign-separate-blank-when-zero-85.exp}
      *>---
      *> kb/Work PB5790. Table 1 of §1985:XIII.3.9.3 SR11 ("Table 1
      *> below shows all permissible clause combinations for a format 3
      *> entry", cite.py: OK; page 605 rendered) marks BLANK WHEN ZERO
      *> and SIGN permitted together in its SOURCE row. The SIGN rule
      *> asks the PICTURE, "a numeric data description entry whose
      *> PICTURE contains the character 'S'" (§1985:XIII.3.17.3 SR1,
      *> cite.py: OK), which S9(3) is though BLANK WHEN ZERO makes the
      *> item numeric edited (§1985:VI.5.4.4 GR2). SEPARATE gives the
      *> sign its own leading or trailing position (§1985:VI.5.12.4
      *> GR6a), so each item is four positions: -12 prints -012, 7
      *> prints 007+, and zero prints nothing but spaces, the sign
      *> position too (§1985:VI.5.4.4 GR1). COBOL-2002 refuses S beside
      *> BLANK WHEN ZERO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5790R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb5790r.txt".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01 WL PIC S9(3) VALUE -12.
       01 WT PIC S9(3) VALUE 7.
       REPORT SECTION.
       RD RPT.
       01 DETL TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC S9(3) SIGN LEADING SEPARATE
             BLANK WHEN ZERO SOURCE WL.
          05 COLUMN 6 PIC S9(3) SIGN TRAILING SEPARATE
             BLANK WHEN ZERO SOURCE WT.
          05 COLUMN 11 PIC X VALUE "X".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE RPT.
           GENERATE DETL.
           MOVE 0 TO WL WT.
           GENERATE DETL.
           TERMINATE RPT.
           CLOSE PRTF.
           DISPLAY "RAN".
           STOP RUN.
