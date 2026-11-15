      *>--- conformance-case
      *> id: pb5337_generate_report_name_runs_detail_use
      *> rules: [GR-14.9.16.4-2, GR-14.9.49.4-9]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5337 - GENERATE report-name of a report with one detail runs that detail's USE BEFORE REPORTING procedure.
      *> §1985:XIII.3.20.4 GR11b (cite.py: OK): for a report with exactly one detail the steps run "as though a GENERATE data-name
      *> statement were being executed", and step b is "If there is a USE BEFORE REPORTING procedure that refers to the data-name
      *> of the report group, the USE procedure is executed"; step a (the subtotalling) precedes it, and §1985:XIII.3.20.4 GR11d
      *> (cite.py: OK) ends the processing there: "processed as a consequence of a GENERATE report-name statement, there is no
      *> further processing done". §2023:14.9.16.4 GR2 (cite.py: OK): "the same processing as for a GENERATE data-name statement
      *> for the same report, except that no detail is printed", and §2023:14.9.49.4 GR9 (cite.py: OK) performs the declarative
      *> "on each occasion that the named report group is processed". The several-details form (2002 on) is the sibling case
      *> pb5337_generate_report_name_several_details.
      *> DERIVATION. R1 has one detail DE-1; S-UP is SUM WS-AMT UPON DE-1. WS-AMT 1.5, WS-SUP "Y", GENERATE R1: S-UP 1.5 first
      *> (GR11a), then the USE procedure (HITS 1, it sees UP 001.5) whose SUPPRESS has nothing left to inhibit; no line prints, so
      *> LINE-COUNTER stays 0. WS-SUP "N", WS-AMT -0.5, GENERATE DE-1: S-UP 1.0, the procedure (HITS 2, UP 001.0), DE-1 prints
      *> (LINE PLUS 1: LINE-COUNTER 1), so the SUPPRESS of the summary GENERATE did not carry over.
      *> Each leg can fail: an engine that skips the procedure for GENERATE R1 displays no USE line and HITS 0; one that runs it
      *> before the subtotal shows UP 000.0; one that prints the detail shows LINE-COUNTER 1 after GENERATE R1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5337U.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5337U.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-KEY   PIC 9    VALUE 1.
       01  WS-AMT   PIC S9V9 VALUE 0.
       01  WS-SUP   PIC X    VALUE "N".
       01  WS-HITS  PIC 9    VALUE 0.
       01  WS-LC    PIC 9    VALUE 0.
       01  WS-D     PIC -999.9.
       REPORT SECTION.
       RD  R1 CONTROLS ARE WS-KEY.
       01  DE-1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  COLUMN 1 PIC -9.9 SOURCE IS WS-AMT.
       01  CF-K TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           02  S-UP  COLUMN 1  PIC -999.9 SUM WS-AMT UPON DE-1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DE-1-USE SECTION.
           USE BEFORE REPORTING DE-1.
       DE-1-USE-P.
           ADD 1 TO WS-HITS.
           MOVE S-UP TO WS-D.
           DISPLAY "USE DE-1 UP=" WS-D.
           IF WS-SUP = "Y"
               SUPPRESS PRINTING
           END-IF.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           MOVE 1.5 TO WS-AMT.
           MOVE "Y" TO WS-SUP.
           GENERATE R1.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "AFTER GEN R1 HITS=" WS-HITS " LC=" WS-LC.
           MOVE "N" TO WS-SUP.
           MOVE -0.5 TO WS-AMT.
           GENERATE DE-1.
           MOVE LINE-COUNTER OF R1 TO WS-LC.
           DISPLAY "AFTER GEN DE1 HITS=" WS-HITS " LC=" WS-LC.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
