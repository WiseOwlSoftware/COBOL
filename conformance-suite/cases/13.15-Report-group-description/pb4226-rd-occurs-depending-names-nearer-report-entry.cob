*>--- conformance-case
*> id: pb4226-rd-occurs-depending-names-nearer-report-entry
*> rules: [SR-13.15.3-7, GR-8.4.6.2.1-3]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB4226 arm 2 - ISO 13.15.3 SR7: an entry's data-name "shall not be referenced in any other way" than in a
*> GENERATE statement, a USE BEFORE REPORTING statement, as a qualifier for a SUM counter, in the UPON phrase of the SUM
*> clause, or as an operand in a SUM clause. PB4226E declares the GLOBAL item N; PB4226F, contained in it, declares its
*> own report entry N and names N in an RD OCCURS ... DEPENDING ON. By ISO 8.4.6.2.1 3) a) the nearer report entry is
*> the referenced item, and the clause may not name it (COBOL5092). Fails if the clause binds the container's N, or is
*> told N is "declared, but not under the given qualifiers" (COBOL1639) when no qualifier is written.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4226E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G IS GLOBAL.
          05 N PIC 99 VALUE 2.
       PROCEDURE DIVISION.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4226F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPTF ASSIGN TO "PB4226F.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD RPTF REPORT IS R1.
       REPORT SECTION.
       RD R1.
       01 DL TYPE DETAIL.
          05 LINE PLUS 1.
             10 N COLUMN 1 PIC 99 SOURCE 5.
             10 COLUMN 5 PIC X VALUE "A"
                OCCURS 1 TO 3 DEPENDING ON N STEP 2.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PB4226F.
       END PROGRAM PB4226E.
