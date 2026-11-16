      *>--- conformance-case
      *> id: g2sum03_sum_rolled_detail_page_footing
      *> rules: [2002-SR-13.16.52.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.52.2 SR3 f) - the two "may" grants no other 2002
      *> accepted case shows: "The current report group may be a detail
      *> and data-name-1 may be defined in a different detail" (T2 in D2
      *> sums X1 of D1) and "The current report group may be a page
      *> footing and data-name-1 may be defined in any body group" (TP in
      *> the page footing sums X1 of D1).
      *>
      *> DERIVATION (2002 text only).  GENERATE D1 with WS-A = 3, then 4,
      *> GENERATE D2, GENERATE D1 with WS-A = 5, TERMINATE.
      *>  D1 lines print X1                              => 03, 04, 05
      *>  §2002:13.16.52.3 GR7 a): "adding takes place when the report
      *>  group description containing data-name-1 is processed", so
      *>  each GENERATE D1 adds X1 into both counters.
      *>  D2 (after two D1): T2 = 3 + 4                  => 007
      *>  Page footing: §2002:13.16.55.3 GR6 f) prints it "as the last
      *>  report group on each page", here at TERMINATE; TP = 3 + 4 + 5
      *>  (printing D2 resets T2 only, GR1 independent counters) => 012
      *> Blank lines of the page are not displayed.
      *> cite.py --check: OK §2002:13.16.52.2 SR3; §2002:13.16.52.3 GR7;
      *>   §2002:13.16.55.3 GR6; §2002:14.8.42.3 GR3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUM03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D
               ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2sum03.txt".
           SELECT CHK ASSIGN TO "g2sum03.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-A    PIC 99    VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE LIMIT 20 LINES FIRST DETAIL 2 LAST DETAIL 15
           FOOTING 18.
       01  D1 TYPE DE LINE PLUS 1.
           05  X1 COLUMN 1 PIC 99 SOURCE WS-A.
       01  D2 TYPE DE LINE PLUS 1.
           05  T2 COLUMN 1 PIC 999 SUM X1.
       01  PFG TYPE PF LINE 19.
           05  TP COLUMN 1 PIC 999 SUM X1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           MOVE 3 TO WS-A.
           GENERATE D1.
           MOVE 4 TO WS-A.
           GENERATE D1.
           GENERATE D2.
           MOVE 5 TO WS-A.
           GENERATE D1.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-X0D
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:WS-I) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
