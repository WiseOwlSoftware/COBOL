      *>--- conformance-case
      *> id: pb5337_generate_report_name_sums_detail_entry
      *> rules: [GR-14.9.16.4-2, GR-13.18.54.4-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5337 - the sole detail a GENERATE report-name PROCESSES adds its entries into the SUM counters that name them.
      *> §2023:14.9.16.4 GR2 (cite.py: OK): summary reporting is "the same processing as for a GENERATE data-name statement for the
      *> same report, except that no detail is printed", and §2023:13.18.54.4 GR7a (cite.py: OK): "adding takes place when the
      *> report group description containing data-name-1 is processed". The detail is processed, so the addition takes place;
      *> the USE BEFORE REPORTING procedure runs first and may compute the entry's source (the order of BeginGroup, kb/Work PB5127).
      *> (Refused at 85: §1985:XIII.3.19.3 SR1, "If identifier-1 is defined in the Report Section, identifier-1 must reference a
      *> sum counter".)
      *> DERIVATION. S-D is SUM D-AMT, D-AMT the detail's SOURCE WS-AMT; the procedure doubles WS-IN into WS-AMT. WS-IN 1.5,
      *> GENERATE R1: the procedure sets WS-AMT 3.0, D-AMT adds 3.0, S-D 3.0, nothing printed. WS-IN 0.5, GENERATE DE-1: WS-AMT 1.0,
      *> S-D 4.0. TERMINATE prints the footing.
      *> Each leg can fail: an engine that does not process the detail on GENERATE R1 shows S-D 000.0 and then 001.0.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5337D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5337D.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       WORKING-STORAGE SECTION.
       01  WS-KEY   PIC 9    VALUE 1.
       01  WS-IN    PIC S9V9 VALUE 0.
       01  WS-AMT   PIC S9V9 VALUE 0.
       01  WS-D     PIC -999.9.
       REPORT SECTION.
       RD  R1 CONTROLS ARE WS-KEY.
       01  DE-1 TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           02  D-AMT COLUMN 1 PIC S9V9 SOURCE IS WS-AMT.
       01  CF-K TYPE IS CONTROL FOOTING WS-KEY LINE NUMBER IS PLUS 1.
           02  S-D   COLUMN 1  PIC -999.9 SUM D-AMT.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DE-1-USE SECTION.
           USE BEFORE REPORTING DE-1.
       DE-1-USE-P.
           COMPUTE WS-AMT = WS-IN * 2.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R1.
           MOVE 1.5 TO WS-IN.
           GENERATE R1.
           MOVE S-D TO WS-D.
           DISPLAY "AFTER GEN R1 SD=" WS-D.
           MOVE 0.5 TO WS-IN.
           GENERATE DE-1.
           MOVE S-D TO WS-D.
           DISPLAY "AFTER GEN DE1 SD=" WS-D.
           TERMINATE R1.
           CLOSE PRT.
           STOP RUN.
