      *>--- conformance-case
      *> id: pb4420-line-counter-spelled-with-dotted-capital-i-is-a-user-word
      *> rules: [GR-8.1.3.2-4, SR-8.4.2.3.3-8]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4420 - ISO 8.1.3.2 4) b): "An uppercase extended
      *> letter is treated as though it were folded to its corresponding
      *> lowercase extended letter, if any, in accordance with the case
      *> mapping described in Annex C" (cite.py --check: OK). LİNE-
      *> COUNTER is not the LINE-COUNTER register, so it is an ordinary
      *> user-defined data-name, and 8.4.2.3.3 SR8 ("In the report
      *> section, neither a sum counter nor the LINE-COUNTER and PAGE-
      *> COUNTER identifiers may be used as a subscript", cite.py
      *> --check: OK) does not bar it as a subscript.
      *> DataBinder.IsReportSubscriptCounter used to compare the word
      *> with LINE-COUNTER through the fold and refused it (COBOL2751).
      *> It fails when that comparison folds U+0130.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4420L.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "PB4420L.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  LİNE-COUNTER PIC 9 VALUE 1.
       01  TB.
           05  TE PIC 9 OCCURS 9 VALUE 1.
       REPORT SECTION.
       RD  R1 PAGE LIMIT 20.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 5 PIC 9 SOURCE TE(LİNE-COUNTER).
       PROCEDURE DIVISION.
           OPEN OUTPUT RPT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPT.
           STOP RUN.
