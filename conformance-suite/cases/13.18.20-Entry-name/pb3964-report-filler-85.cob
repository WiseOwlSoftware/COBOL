*>--- conformance-case
*> id: pb3964-report-filler-85
*> rules: [SR-13.18.20.3-3]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3964 - ANSI X3.23-1985's report group description entry is "level-number [data-name-1]": an
      *> unnamed entry omits the name. ISO/IEC 1989:2002 Annex F.2 item 53 (post-ballot text v200112.txt line 44025):
      *> "FILLER. FILLER is allowed in the report section." The entry 02 FILLER below names a report item with the
      *> word FILLER, so at --std 85 it is refused with COBOL0900 (construct report-filler-2002, VCR row 7.45). The
      *> 2023 text, 13.18.20.3 SR3 (cite.py: OK 13.18.20.3 3)), makes the entry name "either data-name-1 or FILLER";
      *> the 2002 positive is 13.15-Report-group-description/pb1226_report_filler_entry_name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3964N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB3964N1.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE PLUS 1.
           02  FILLER COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
