      *>--- conformance-case
      *> id: g85_f29_07
      *> rules: [1985-GR-XIII.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.3.4 GR4 - "an automatic function of the report
      *> writer control system is to process page heading and page
      *> footing report groups, if defined, when the report writer
      *> control system must advance the report to a new page for the
      *> purpose of presenting a body group." Both a page heading and
      *> a page footing are defined, and both are processed on the
      *> advance (U-PF, then U-PH); the body group that forces the
      *> advance is a control footing produced by GENERATE.
      *> DERIVATION. PAGE LIMIT 10 HEADING 1 FIRST DETAIL 2 LAST
      *> DETAIL 6 FOOTING 7; PH LINE 1, DE LINE PLUS 1, CF LINE PLUS
      *> 2, PF LINE 9.
      *>   First GENERATE: page heading processed (U-PH PC 1 LC 0,
      *>     §1985:XIII.4.3.4 GR5b), presented on line 1.
      *>   GENERATE x5 (K = A): first detail on line 2 (LC 1 < FIRST
      *>     DETAIL, §1985:XIII.3.10.8 GR4b), then 3, 4, 5, 6 (each
      *>     trial sum <= LAST DETAIL 6). LC 6.
      *>   K = B, GENERATE: control break, CF processed: U-CF; fit
      *>     test 3b: 6 + 2 = 8 > FOOTING 7 (the CF's lower limit,
      *>     GR2), so page advance: U-PF PC 1 LC 6, page advance (PC 2,
      *>     LC 0), U-PH PC 2 LC 0, PH on line 1; CF is the first
      *>     body group of page 2: line 2 (GR4b), LC 2 (GR6d). Then
      *>     U-DE B; 2 + 1 = 3.
      *>     PC 2, LC 3.
      *> The PF and PH USE lines are shown only before TERMINATE.
      *> Fails if the page footing or the page heading is not
      *> processed on the advance, or if the counters show no advance.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2907.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85f2907.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-K            PIC X     VALUE "A".
       01  WS-SHOW         PIC X     VALUE "Y".
       01  WS-LC           PIC 99    VALUE 0.
       01  WS-PC           PIC 99    VALUE 0.
       01  WS-J            PIC 9     VALUE 0.
       REPORT SECTION.
       RD  RPT CONTROL IS WS-K
           PAGE LIMIT IS 10 LINES HEADING 1 FIRST DETAIL 2
           LAST DETAIL 6 FOOTING 7.
       01  PH-G TYPE PH LINE 1.
           02  COLUMN 1 PIC X(2) VALUE "PH".
       01  DE-G TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(2) VALUE "DE".
           02  COLUMN 4 PIC X SOURCE WS-K.
       01  CF-G TYPE CF WS-K LINE PLUS 2.
           02  COLUMN 1 PIC X(2) VALUE "CF".
       01  PF-G TYPE PF LINE 9.
           02  COLUMN 1 PIC X(2) VALUE "PF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-PH SECTION.
           USE BEFORE REPORTING PH-G.
       U-PH-P.
           IF WS-SHOW = "Y"
               MOVE PAGE-COUNTER TO WS-PC
               MOVE LINE-COUNTER TO WS-LC
               DISPLAY "U-PH PC " WS-PC " LC " WS-LC
           END-IF.
       U-DE SECTION.
           USE BEFORE REPORTING DE-G.
       U-DE-P.
           DISPLAY "U-DE " WS-K.
       U-CF SECTION.
           USE BEFORE REPORTING CF-G.
       U-CF-P.
           IF WS-SHOW = "Y"
               DISPLAY "U-CF"
           END-IF.
       U-PF SECTION.
           USE BEFORE REPORTING PF-G.
       U-PF-P.
           IF WS-SHOW = "Y"
               MOVE PAGE-COUNTER TO WS-PC
               MOVE LINE-COUNTER TO WS-LC
               DISPLAY "U-PF PC " WS-PC " LC " WS-LC
           END-IF.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           PERFORM 5 TIMES
               GENERATE DE-G
           END-PERFORM.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           MOVE "B" TO WS-K.
           GENERATE DE-G.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "AFTER PC " WS-PC " LC " WS-LC.
           MOVE "N" TO WS-SHOW.
           TERMINATE RPT.
           CLOSE PRT.
           STOP RUN.
