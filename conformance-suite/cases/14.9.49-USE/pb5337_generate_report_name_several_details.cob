      *>--- conformance-case
      *> id: pb5337_generate_report_name_several_details
      *> rules: [GR-14.9.16.4-2, GR-14.9.49.4-9]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5337 - GENERATE report-name of a report with SEVERAL details runs no detail's USE BEFORE REPORTING procedure.
      *> §2023:14.9.16.4 GR2 (cite.py: OK) gives the statement "the same processing as for a GENERATE data-name statement for the
      *> same report, except that no detail is printed" and names no detail when the report has several; the pinned choice of
      *> docs/CONFORMANCE.md (kb/Work PB5249) is that such a statement processes none, so §2023:14.9.49.4 GR9's "on each occasion
      *> that the named report group is processed" never comes for either. (At --std 85 the form is refused, §1985:XIII.3.20.3 SR6.)
      *> The one-detail case is pb5337_generate_report_name_runs_detail_use.
      *> DERIVATION. GENERATE R2: no detail processed, HITS 0. GENERATE D2-A: its procedure runs, HITS 1.
      *> The leg can fail: an engine that picks a detail for the summary GENERATE (GnuCOBOL runs D2-A's) shows HITS 1, then 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5337S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5337S.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R2.
       WORKING-STORAGE SECTION.
       01  WS-KEY   PIC 9    VALUE 1.
       01  WS-HITS  PIC 9    VALUE 0.
       REPORT SECTION.
       RD  R2 CONTROLS ARE WS-KEY.
       01  D2-A TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC X VALUE "A".
       01  D2-B TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC X VALUE "B".
       PROCEDURE DIVISION.
       DECLARATIVES.
       D2-A-USE SECTION.
           USE BEFORE REPORTING D2-A.
       D2-A-USE-P.
           ADD 1 TO WS-HITS.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R2.
           GENERATE R2.
           DISPLAY "AFTER GEN R2 HITS=" WS-HITS.
           GENERATE D2-A.
           DISPLAY "AFTER GEN D2A HITS=" WS-HITS.
           TERMINATE R2.
           CLOSE PRT.
           STOP RUN.
