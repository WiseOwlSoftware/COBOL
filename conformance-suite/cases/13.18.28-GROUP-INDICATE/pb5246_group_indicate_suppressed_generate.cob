      *>--- conformance-case
      *> id: pb5246_group_indicate_suppressed_generate
      *> rules: [GR-13.18.28.4-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5246 - GROUP INDICATE after a GENERATE that SUPPRESS PRINTING kept from printing. Two editions, two rules:
      *>   §2023:13.18.28.4 GR1 (cite.py: OK; the 2002 text, 13.16.26.3 GR1, the same): GROUP INDICATE "has the same effect as a PRESENT WHEN
      *>   clause where the associated condition is true only on the first occasion that a GENERATE is issued for the current detail group
      *>   after" an INITIATE, a page advance or a control break. A GENERATE whose group the USE procedure suppressed WAS issued, so it
      *>   uses the occasion up.  §1985:XIII.3.13.4 GR1 (cite.py: OK): GROUP INDICATE "causes the SOURCE or VALUE clause to be ignored and
      *>   spaces supplied, except: a. On the first presentation of the detail report group in the report, ... c. On the first presentation
      *>   of the detail report group after every control break", and §1985:XIII.4.6.4 GR3 (cite.py: OK) says a SUPPRESS statement inhibits
      *>   "The presentation of the print lines of the report group": a suppressed GENERATE is no presentation, so the occasion is still owed.
      *> DERIVATION. WS-N 1 and 4 are suppressed. GENERATE 1 (suppressed), 2, 3; the control item WS-K changes to "B" (a control break:
      *> the indication is armed again); GENERATE 4 (suppressed), 5. The indicated items are the VALUE "AA" in columns 1-2 and WS-K in
      *> column 4; WS-N (column 6) prints always.
      *>   COBOL-85: the first presentation is GENERATE 2 ("AA A 2"), GENERATE 3 has spaces, the first one after the break is GENERATE 5.
      *>   COBOL-2002 and later: GENERATE 1 consumed the occasion, so 2 and 3 have spaces; GENERATE 4 consumed the one after the break, so
      *>   5 has spaces. Each leg can fail: an engine that applies either rule at the other edition changes the first and third lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5246G.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0D ARE 11 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB5246G.TXT".
           SELECT CHK ASSIGN TO "PB5246G.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K    PIC X     VALUE "A".
       01  WS-N    PIC 9     VALUE 0.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  RPT CONTROL IS WS-K PAGE LIMIT IS 20 LINES.
       01  DL TYPE IS DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN NUMBER IS 1 PIC XX VALUE "AA" GROUP INDICATE.
           05  COLUMN NUMBER IS 4 PIC X SOURCE WS-K GROUP INDICATE.
           05  COLUMN NUMBER IS 6 PIC 9 SOURCE WS-N.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE BEFORE REPORTING DL.
       D1-P.
           IF WS-N = 1 OR WS-N = 4
               SUPPRESS PRINTING
           END-IF.
       END DECLARATIVES.
       M SECTION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           MOVE 1 TO WS-N. GENERATE DL.
           MOVE 2 TO WS-N. GENERATE DL.
           MOVE 3 TO WS-N. GENERATE DL.
           MOVE "B" TO WS-K.
           MOVE 4 TO WS-N. GENERATE DL.
           MOVE 5 TO WS-N. GENERATE DL.
           TERMINATE RPT.
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
