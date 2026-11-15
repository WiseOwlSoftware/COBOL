      *>--- conformance-case
      *> id: g2-generate-first-and-later
      *> rules: [2002-GR-14.8.15.3-4, 2002-GR-14.8.15.3-5,
      *>    2002-SR-14.8.15.2-1, 2002-SR-14.8.15.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.15.3 GR4 - the chronologically first GENERATE after
      *>   an INITIATE prints, in order: a) the report heading (and, if it
      *>   is on a page by itself, "an advance is made to the next
      *>   physical page, and PAGE-COUNTER is either incremented by 1 or,
      *>   if the report heading's NEXT GROUP clause has the WITH RESET
      *>   phrase, set to 1"); b) a page heading; c) "each control
      *>   heading is printed, wherever defined, in order from major to
      *>   minor"; d) "The specified detail is printed, unless summary
      *>   reporting is specified".
      *> §2002:14.8.15.3 GR5 - later GENERATEs: a) on a control break
      *>   "each control footing and control heading is printed, if
      *>   defined, up to the level of the control break"; b) the detail
      *>   unless summary reporting.
      *> §2002:14.8.15.2 SR1 - data-name-1 names a detail group and "may
      *>   be qualified by a report-name" (GENERATE DET OF R-G, accepted).
      *> §2002:14.8.15.2 SR2 - report-name-1 is used where the RD has a
      *>   CONTROL clause (GENERATE R-G, accepted; GR2: "no detail is
      *>   printed").
      *> DERIVATION. INITIATE: PAGE-COUNTER 1 (§2002:14.8.20.3 GR1c).
      *> R-G, CONTROLS FINAL WS-K (FINAL is the highest level,
      *>   §2002:13.16.15.3 GR2); RH LINE 1, PH LINE 2, so the RH is not
      *>   alone on its page.
      *>  GEN 1 (K=A N=1, the first): GR4 a-d: [RH] [PH 1] [CHF] [CHK A]
      *>   [DE A 1].
      *>  GEN 2 (DET OF R-G, no break): GR5 b: [DE A 2].
      *>  GEN 3 (K=B): break at WS-K (§2002:13.16.15.3 GR3: "control
      *>   break processing for that level and any lower levels"), FINAL
      *>   not broken. GR5 a: CF WS-K sees the prior value
      *>   (§2002:13.16.15.3 GR4a) [CFK A], then CH [CHK B]; GR5 b
      *>   [DE B 3].
      *>  GEN 4 (GENERATE R-G, K=C): GR5 a [CFK B] [CHK C]; GR2/GR5 b:
      *>   no detail, so no "DE C 4".
      *>  TERMINATE (§2002:14.8.42.3 GR3a-b): control items take prior
      *>   values, footings minor first as a most-major break: [CFK C]
      *>   [CFF]. No PF or RF is defined.
      *> R-P: RH NEXT GROUP NEXT PAGE - "the report heading is printed on
      *>   the first page of the report as the only report group on that
      *>   page" (§2002:13.16.35.3 GR3c); GR4 a: page advance and
      *>   PAGE-COUNTER incremented: [RH P] / page / [PH 2] [DE P].
      *> R-Q: the same WITH RESET - GR4 a and §2002:13.16.35.3 GR6:
      *>   PAGE-COUNTER set to 1: [RH Q] / page / [PH 1] [DE Q].
      *> EACH LEG CAN FAIL: CH printed minor first puts CHK A before CHF;
      *>   a CF that sees the new value prints CFK B for CFK A; summary
      *>   reporting that prints a detail adds DE C 4; an RH that shares
      *>   its page drops the page line; an advance that leaves
      *>   PAGE-COUNTER alone prints PH 1 for R-P, and an ignored RESET
      *>   prints PH 2 for R-Q. The reader shows non-blank lines in order
      *>   and a form feed as "---- PAGE ----".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GEN01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRTG ASSIGN TO "G2GEN01G.TXT".
           SELECT PRTP ASSIGN TO "G2GEN01P.TXT".
           SELECT PRTQ ASSIGN TO "G2GEN01Q.TXT".
           SELECT CHK ASSIGN USING WS-FN.
       DATA DIVISION.
       FILE SECTION.
       FD  PRTG REPORT IS R-G.
       FD  PRTP REPORT IS R-P.
       FD  PRTQ REPORT IS R-Q.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-FN   PIC X(12) VALUE SPACES.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-K    PIC X     VALUE "A".
       01  WS-N    PIC 9     VALUE 0.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R-G CONTROLS ARE FINAL WS-K
           PAGE LIMIT 40 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 30 FOOTING 35.
       01  TYPE RH.
           02 LINE 1. 03 COLUMN 1 PIC X(2) VALUE "RH".
       01  TYPE PH.
           02 LINE 2. 03 COLUMN 1 PIC X(2) VALUE "PH".
                      03 COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  TYPE CH FINAL.
           02 LINE PLUS 1. 03 COLUMN 1 PIC X(3) VALUE "CHF".
       01  TYPE CH WS-K.
           02 LINE PLUS 1. 03 COLUMN 1 PIC X(3) VALUE "CHK".
                           03 COLUMN 5 PIC X SOURCE WS-K.
       01  DET TYPE DE.
           02 LINE PLUS 1. 03 COLUMN 1 PIC X(2) VALUE "DE".
                           03 COLUMN 4 PIC X SOURCE WS-K.
                           03 COLUMN 6 PIC 9 SOURCE WS-N.
       01  TYPE CF WS-K.
           02 LINE PLUS 1. 03 COLUMN 1 PIC X(3) VALUE "CFK".
                           03 COLUMN 5 PIC X SOURCE WS-K.
       01  TYPE CF FINAL.
           02 LINE PLUS 1. 03 COLUMN 1 PIC X(3) VALUE "CFF".
       RD  R-P
           PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01  TYPE RH NEXT GROUP NEXT PAGE.
           02 LINE 1. 03 COLUMN 1 PIC X(4) VALUE "RH P".
       01  TYPE PH.
           02 LINE 1. 03 COLUMN 1 PIC X(2) VALUE "PH".
                      03 COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  DET-P TYPE DE.
           02 LINE PLUS 1. 03 COLUMN 1 PIC X(4) VALUE "DE P".
       RD  R-Q
           PAGE LIMIT 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15.
       01  TYPE RH NEXT GROUP NEXT PAGE WITH RESET.
           02 LINE 1. 03 COLUMN 1 PIC X(4) VALUE "RH Q".
       01  TYPE PH.
           02 LINE 1. 03 COLUMN 1 PIC X(2) VALUE "PH".
                      03 COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  DET-Q TYPE DE.
           02 LINE PLUS 1. 03 COLUMN 1 PIC X(4) VALUE "DE Q".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT PRTG.
           INITIATE R-G.
           MOVE "A" TO WS-K. MOVE 1 TO WS-N.
           GENERATE DET.
           MOVE 2 TO WS-N.
           GENERATE DET OF R-G.
           MOVE "B" TO WS-K. MOVE 3 TO WS-N.
           GENERATE DET.
           MOVE "C" TO WS-K. MOVE 4 TO WS-N.
           GENERATE R-G.
           TERMINATE R-G.
           CLOSE PRTG.
           OPEN OUTPUT PRTP.
           INITIATE R-P.
           GENERATE DET-P.
           TERMINATE R-P.
           CLOSE PRTP.
           OPEN OUTPUT PRTQ.
           INITIATE R-Q.
           GENERATE DET-Q.
           TERMINATE R-Q.
           CLOSE PRTQ.
           MOVE "G2GEN01G.TXT" TO WS-FN.
           DISPLAY "== R-G ==".
           PERFORM SHOW-FILE.
           MOVE "G2GEN01P.TXT" TO WS-FN.
           DISPLAY "== R-P ==".
           PERFORM SHOW-FILE.
           MOVE "G2GEN01Q.TXT" TO WS-FN.
           DISPLAY "== R-Q ==".
           PERFORM SHOW-FILE.
           STOP RUN.
       SHOW-FILE.
           MOVE "N" TO WS-EOF.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN CHK-REC = X"0A"
                   PERFORM SHOW-LINE
               WHEN CHK-REC = X"0C"
                   PERFORM SHOW-LINE
                   DISPLAY "---- PAGE ----"
               WHEN CHK-REC = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE(1:WS-I) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
