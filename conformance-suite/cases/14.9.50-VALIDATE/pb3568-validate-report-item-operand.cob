      *>--- conformance-case
      *> id: pb3568-validate-report-item-operand
      *> rules: [SR-14.9.50.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> An identifier-1 described in the REPORT SECTION (a sum counter, SUM W), not in one of the four sections the
      *> rule lists. Expected: COBOL4777.
      *> ISO §14.9.50.3 SR1 - "Identifier-1 shall reference a data item described in the file, linkage,
      *> local-storage, or working-storage section. Identifier-1 shall not be reference-modified."
      *>   cite.py: OK  §14.9.50.3 1)  (Syntax rules)
      *> The VALIDATE facility is DECLINED (COBOL1580, docs/CONFORMANCE.md section 4 item 3): the statement is
      *> accepted inert. Declining the facility declines its BEHAVIOUR, never its syntax rules - the operand of the
      *> statement is still an identifier (kb/Work PB3568). The statement is the only violation; the program is
      *> otherwise legal. VALIDATE exists from COBOL-2002; at --std 85 it is a user word, so no 85 refusal is named.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "pb3568c.dat"
               ORGANIZATION IS LINE SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD PRT REPORT IS RP.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       REPORT SECTION.
       RD RP PAGE LIMIT 10 LINES.
       01 DET TYPE DETAIL LINE PLUS 1.
          05 COLUMN 1 PIC 9 SOURCE W.
          05 RGI COLUMN 5 PIC 9 SUM W.
       PROCEDURE DIVISION.
       MAIN-P.
           VALIDATE RGI.
           STOP RUN.
