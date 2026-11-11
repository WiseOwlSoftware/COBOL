*>--- conformance-case
*> id: pb5149-report-name-and-global-data-name
*> rules: [SR-8.3.2.2-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5149 - one word as a report-name and as a GLOBAL data-name of one source element. ISO 8.3.2.2: "Within a
      *> source element, a given user-defined word may be used as only one type of user-defined word" (cite.py --check
      *> 8.3.2.2 "a given user-defined word may be used as only one type of user-defined word"); a report-name is not in
      *> the one group the rule lets share a name (constant-name, data-name, property-name, record-key-name,
      *> record-name). The note supposed the GLOBAL-name screen was the only guard of the pair; the user-word census is
      *> the guard of every type of name, so the pair is refused whether or not the data-name is GLOBAL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEGPB5149A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OUT-FILE ASSIGN TO "pb5149.out".
       DATA DIVISION.
       FILE SECTION.
       FD  OUT-FILE REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  RPT PIC X VALUE "A" GLOBAL.
       REPORT SECTION.
       RD  RPT PAGE LIMIT 10 LINES.
       01  DET TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "OK".
           STOP RUN.
