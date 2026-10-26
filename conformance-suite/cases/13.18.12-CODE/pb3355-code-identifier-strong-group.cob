*>--- conformance-case
*> id: pb3355-code-identifier-strong-group
*> rules: [SR-13.18.12.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3355. ISO/IEC 1989:2023 13.18.12.3 SR2: "Identifier-1 shall reference an alphanumeric data item that
      *> shall not be an occurs-depending-on group item, a variable-length group, or a dynamic-length elementary
      *> item." (cite.py --check 13.18.12.3 "Identifier-1 shall reference an alphanumeric data item" -> OK). 8.5.2.1:
      *> "Both the class and the category of a strongly-typed group item are the type-name specified in the TYPE
      *> clause" (cite.py -> OK), so the strongly-typed group V is not an alphanumeric data item (COBOL2713).
      *> Why this can fail: the CODE screen asked the class lattice, which answers alphanumeric for a strong group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3355CD1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "pb3355-cd1.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 REC-T IS TYPEDEF STRONG.
          05 F1 PIC X(2).
       01 V TYPE REC-T.
       REPORT SECTION.
       RD R-1 CODE IS V.
       01 DET-A TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X VALUE "A".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT RPT
           INITIATE R-1
           GENERATE DET-A
           TERMINATE R-1
           CLOSE RPT
           STOP RUN.
