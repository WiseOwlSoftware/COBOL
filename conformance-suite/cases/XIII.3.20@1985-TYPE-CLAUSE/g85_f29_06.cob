      *>--- conformance-case
      *> id: g85_f29_06
      *> rules: [1985-GR-XIII.3.20.4-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.4 GR12 - while processing a detail group the
      *> RWCS "may have to interrupt the processing of that body group
      *> after determining that the body group is to be presented, and
      *> execute a page advance (and process page footing and page
      *> heading report groups) before actually presenting the body
      *> group." The detail's USE procedure runs first (GR11b: it is
      *> part of determining whether the group is presented), then the
      *> page footing, the page advance, the page heading, and only
      *> then the detail is presented on the new page.
      *> DERIVATION. PAGE LIMIT 12 HEADING 1 FIRST DETAIL 3 LAST
      *> DETAIL 8 FOOTING 10; PH LINE 1, PF LINE 11, DE LINE PLUS 2.
      *>   GENERATE 1: PH first (§1985:XIII.4.3.4 GR5b): U-PH PC 1 LC 0
      *>     (§1985:XIII.4.4.4 GR1). DE 1 is the first body group and
      *>     LC < FIRST DETAIL, so it goes on line 3 (§1985:XIII.3.10.8
      *>     GR4b). LC 3.
      *>   GENERATE 2, 3: 3 + 2 = 5, 5 + 2 = 7. LC 7.
      *>   GENERATE 4: U-DE 4; fit test 3b: 7 + 2 = 9 > LAST DETAIL 8,
      *>     so page advance processing: U-PF PC 1 LC 7 (PF on line 11
      *>     by §1985:XIII.3.15.4 GR3), page advance (LC to 0,
      *>     §1985:XIII.3.5.6 GR4; PAGE-COUNTER + 1,
      *>     §1985:XIII.3.5.5 GR5), U-PH PC 2 LC 0, then DE 4 is the
      *>     first body group of page 2: line 3. PC 2, LC 3.
      *> The PF USE line is shown only before TERMINATE, so the trace
      *> does not depend on what TERMINATE does with the last page.
      *> Fails if the page footing or heading run before the detail's
      *> USE procedure, if no page advance is taken, or if the detail
      *> is presented before the page heading.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2906.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85f2906.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-V            PIC 9     VALUE 0.
       01  WS-SHOW         PIC X     VALUE "Y".
       01  WS-LC           PIC 99    VALUE 0.
       01  WS-PC           PIC 99    VALUE 0.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 12 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 8 FOOTING 10.
       01  PH-G TYPE PH LINE 1.
           02  COLUMN 1 PIC X(2) VALUE "PH".
       01  DE-G TYPE DE LINE PLUS 2.
           02  COLUMN 1 PIC X(2) VALUE "DE".
           02  COLUMN 4 PIC 9 SOURCE WS-V.
       01  PF-G TYPE PF LINE 11.
           02  COLUMN 1 PIC X(2) VALUE "PF".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-PH SECTION.
           USE BEFORE REPORTING PH-G.
       U-PH-P.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "U-PH PC " WS-PC " LC " WS-LC.
       U-DE SECTION.
           USE BEFORE REPORTING DE-G.
       U-DE-P.
           DISPLAY "U-DE " WS-V.
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
           MOVE 1 TO WS-V.
           GENERATE DE-G.
           MOVE 2 TO WS-V.
           GENERATE DE-G.
           MOVE 3 TO WS-V.
           GENERATE DE-G.
           MOVE 4 TO WS-V.
           GENERATE DE-G.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "AFTER PC " WS-PC " LC " WS-LC.
           MOVE "N" TO WS-SHOW.
           TERMINATE RPT.
           CLOSE PRT.
           STOP RUN.
