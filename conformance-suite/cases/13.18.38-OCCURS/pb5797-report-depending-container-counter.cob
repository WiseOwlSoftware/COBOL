*>--- conformance-case
*> id: pb5797-report-depending-container-counter
*> rules: [SR-13.18.38.3-18]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5797 - ISO 13.18.38.3 SR18 (Formats 2 and 3): under the GLOBAL RD RPT of the contained
      *> program, the DEPENDING object NG shall be a global name "described in the same data division". NG
      *> is global but is described in the CONTAINING program's data division. (The report OCCURS clause
      *> arrived in COBOL 2002.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5797OR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NG PIC 9 GLOBAL VALUE 2.
       PROCEDURE DIVISION.
       P1.
           CALL "PB5797IR"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5797IR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "PB5797IR.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD PRTF REPORT IS RPT.
       REPORT SECTION.
       RD RPT GLOBAL.
       01 TYPE DETAIL.
           02 LINE PLUS 1.
               03 COLUMN 1 PIC X VALUE "A" OCCURS 1 TO 3 DEPENDING ON NG
                  STEP 2.
       PROCEDURE DIVISION.
       Q1.
           EXIT PROGRAM.
       END PROGRAM PB5797IR.
       END PROGRAM PB5797OR.
