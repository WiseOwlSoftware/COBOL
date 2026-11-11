      *>--- conformance-case
      *> id: pb5245_terminate_saved_next_group
      *> rules: [GR-13.18.37.4-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5245 - a saved absolute NEXT GROUP integer and the control footings TERMINATE produces. §2023:13.18.37.4 GR4 a) (cite.py: OK;
      *> the 2002 text, 13.16.35.3 GR4 a), the same): the integer placed in the save location "will have no effect at all if a TERMINATE is next
      *> executed for the report". §1985:XIII.3.10.8 GR3 (cite.py: OK): after a page advance the report writer control system "moves the saved
      *> next group integer into LINE-COUNTER, resets saved next group integer to zero, and reapplies fit test rule 3a", and §1985:XIII.4.7.4
      *> GR1 (cite.py: OK) has TERMINATE "produce all the control footing report groups": body groups, so they meet the saved integer.
      *> DERIVATION. PAGE LIMIT 20, HEADING 1, FIRST DETAIL 5, LAST DETAIL 12, FOOTING 16. GENERATE 1 prints ITEM on line 5 (the first body
      *> group is exempt from the fit test) and NEXT GROUP 10 takes LINE-COUNTER from 5 to 10. GENERATE 2: 10 + 1 = 11 does not pass LAST
      *> DETAIL 12, so ITEM prints on line 11, and NEXT GROUP 10 finds LINE-COUNTER 11 not below 10, so 10 is saved and LINE-COUNTER becomes
      *> FOOTING, 16. TERMINATE, the footing TOTAL (LINE PLUS 1):
      *>   COBOL-2002 and later: the saved integer is discarded and LINE-COUNTER restored to 11, so TOTAL fits (12 does not pass FOOTING 16)
      *>   and prints on line 12 of page 1: PAGE-COUNTER 1, LINE-COUNTER 12.
      *>   COBOL-85: LINE-COUNTER 16 + 1 = 17 passes FOOTING 16, the page advances (PAGE-COUNTER 2), the saved 10 goes into LINE-COUNTER and
      *>   the fit test is reapplied (10 + 1 = 11 fits): TOTAL prints on line 11 of page 2: PAGE-COUNTER 2, LINE-COUNTER 11.
      *> Each leg can fail: a TERMINATE that discards the saved integer at COBOL-85, or keeps it from 2002 on, changes both numbers.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5245T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5245T.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  WS-P    PIC 99    VALUE 0.
       01  WS-L    PIC 99    VALUE 0.
       REPORT SECTION.
       RD  RPT CONTROL IS FINAL PAGE LIMIT IS 20 LINES HEADING 1
           FIRST DETAIL 5 LAST DETAIL 12 FOOTING 16.
       01  DL TYPE IS DETAIL LINE PLUS 1 NEXT GROUP 10.
           05  COLUMN 1 PIC X(4) VALUE "ITEM".
       01  CFF TYPE IS CONTROL FOOTING FINAL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "TOTAL".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           GENERATE DL.
           GENERATE DL.
           TERMINATE RPT.
           MOVE PAGE-COUNTER OF RPT TO WS-P.
           MOVE LINE-COUNTER OF RPT TO WS-L.
           DISPLAY "PAGE " WS-P " LINE " WS-L.
           CLOSE PRT.
           STOP RUN.
