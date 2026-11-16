      *>--- conformance-case
      *> id: pb5823_saved_next_group_fit_test
      *> rules: [GR-13.18.37.4-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5823 - the group after a saved absolute NEXT GROUP integer. §2023:13.18.37.4 GR4a (cite.py: OK; the 2002 text,
      *> 13.16.35.3 GR4 a), the same): LINE-COUNTER is set to FOOTING, "causing a page advance to take place just before any other
      *> non-dummy body group is printed". §1985:XIII.3.10.8 GR3b (cite.py: OK): "If a body group has been presented on the page to which the
      *> report is currently positioned, the RWCS computes a trial sum in a work location", and "If the trial sum is not greater than the body
      *> group's lower limit integer, then the report group is presented on the current page"; only "If no body group has yet been presented on
      *> the page" does the RWCS read the saved integer (§1985:XIII.3.10.8 GR3b second paragraph), and §1985:XIII.3.10.8 GR4b (cite.py: OK)
      *> puts the group on "the line that is obtained by adding the content of LINE-COUNTER and the integer of the first LINE NUMBER clause".
      *> DERIVATION. PAGE LIMIT 15, HEADING 1, FIRST DETAIL 3, LAST DETAIL 10, FOOTING 12. DE-A is LINE PLUS 1 NEXT GROUP 8.
      *> GENERATE 1: DE-A prints on line 3 (FIRST DETAIL); NEXT GROUP 8 against line 3: LINE-COUNTER becomes 8. GENERATE 2: 8 + 1 = 9 does not
      *> pass LAST DETAIL 10, DE-A prints on line 9; its NEXT GROUP 8 is not above line 9, so 8 goes into the save location and LINE-COUNTER
      *> is FOOTING, 12. The control K changes to 2: the control footing CF-K is LINE PLUS 0 and prints first (the break).
      *>   COBOL-85: a body group is on page 1, so the trial sum 12 + 0 = 12 is not greater than FOOTING 12 and CF-K prints on page 1, line 12
      *>   (LINE-COUNTER 12). DE-A (LINE PLUS 1) then has trial sum 13, which passes LAST DETAIL 10: the page advances (PAGE-COUNTER 2), the saved
      *>   8 goes into LINE-COUNTER and DE-A prints on line 9; its NEXT GROUP 8 saves 8 again and LINE-COUNTER is 12. TERMINATE: the
      *>   control footing CF-K (LINE PLUS 0) fits on line 12 of page 2: PAGE-COUNTER 2, LINE-COUNTER 12.
      *>   COBOL-2002 and later: the saved integer forces the page advance before CF-K, which prints on line 9 of page 2 (saved 8 + 1), and DE-A
      *>   follows on line 10; the TERMINATE footing discards the saved integer. PAGE-COUNTER 2, LINE-COUNTER 10.
      *> The program shows PAGE-COUNTER and LINE-COUNTER after each statement. Each leg can fail: a forced advance at COBOL-85 or a footing
      *> that stays on page 1 from 2002 on changes the fourth line (the page the TERMINATE footing lands on: 3 against 2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5823T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5823T.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RP.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC 9     VALUE 1.
       01  WS-LC   PIC 99.
       01  WS-PC   PIC 99.
       REPORT SECTION.
       RD  RP CONTROL IS WS-K
                PAGE LIMIT 15 LINES HEADING 1 FIRST DETAIL 3
                LAST DETAIL 10 FOOTING 12.
       01  DE-A TYPE DE LINE PLUS 1 NEXT GROUP 8.
           02  COLUMN 1 PIC XX VALUE "DA".
           02  COLUMN 4 PIC 99 SOURCE LINE-COUNTER.
       01  CF-K TYPE CF WS-K LINE PLUS 0.
           02  COLUMN 10 PIC XX VALUE "CF".
           02  COLUMN 13 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       P0.
           OPEN OUTPUT PRT.
           INITIATE RP.
           GENERATE DE-A. PERFORM SHOW.
           GENERATE DE-A. PERFORM SHOW.
           MOVE 2 TO WS-K.
           GENERATE DE-A. PERFORM SHOW.
           TERMINATE RP. PERFORM SHOW.
           CLOSE PRT.
           STOP RUN.
       SHOW.
           MOVE LINE-COUNTER TO WS-LC.
           MOVE PAGE-COUNTER TO WS-PC.
           DISPLAY "PC " WS-PC " LC " WS-LC.
