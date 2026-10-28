*>--- conformance-case
*> id: pb3964-report-plus-symbol-85
*> rules: [SR-13.18.35.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3964 - ANSI X3.23-1985 prints the relative LINE operand as the word PLUS alone. ISO/IEC 1989:2002
      *> Annex F.2 item 115 (post-ballot text v200112.txt line 44269) lists "PLUS or MINUS can be the symbol '+' or '-',
      *> respectively". LINE + 1 below writes the symbol, so at --std 85 it is refused with COBOL0900 (construct
      *> report-plus-symbol-2002, VCR row 7.45). The 2023 text, 13.18.35.3 SR1 (cite.py: OK 13.18.35.3 1), is "PLUS
      *> and + are synonyms."; the 2002 positives are 8.4.3.15-Report-counters/pb957_next_group_forms (LINE and NEXT
      *> GROUP). No MINUS operand exists in any 2023 relative format, so the '-' half of the item names nothing to gate.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3964N2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB3964N2.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  DA TYPE DE LINE + 1.
           02  COLUMN 1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DA.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
