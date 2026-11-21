      *>--- conformance-case
      *> id: pb6027-report-fd-data-records-85
      *> rules: [1985-FMT-XIII.3.2.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6027. The report file's file description entry
      *> (1985:XIII.3.2.2, and with EXTERNAL and GLOBAL 1985:X.4.2.2
      *> Format 3, both rendered from the printed pages) prints BLOCK
      *> CONTAINS, RECORD, LABEL RECORDS, VALUE OF, CODE-SET and REPORT,
      *> and no DATA RECORDS clause, which only the sequential, relative
      *> and indexed entries print (1985:VII.3.2.2, VIII.3.2.2, IX.3.2.2).
      *> 1985:XIII.3.2.3 SR4: "No record description entries may follow
      *> the file description entry for a report file." So DATA RECORD
      *> IS X is refused at 85 as a clause the format does not contain
      *> (COBOL2604). COBOL-2002 deleted the clause (COBOL0903 there).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6027A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTF ASSIGN TO "pb6027a.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRTF DATA RECORD IS X REPORT IS R1.
       REPORT SECTION.
       RD  R1.
       01  DA TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRTF.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRTF.
           STOP RUN.
