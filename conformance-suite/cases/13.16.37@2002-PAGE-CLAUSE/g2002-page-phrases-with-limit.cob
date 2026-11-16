      *>--- conformance-case
      *> id: g2002-page-phrases-with-limit
      *> rules: [2002-SR-13.16.37.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.37.2 SR3 - "The HEADING, FIRST DETAIL, LAST CONTROL
      *> HEADING, LAST DETAIL, or FOOTING phrase may be specified only if
      *> integer-1 is specified." The RD below specifies integer-1 (PAGE
      *> LIMIT IS 10 LINES) and all five phrases; the program must compile,
      *> and each phrase must take effect. The integers ascend (SR6).
      *> (The refusal, a phrase without integer-1, is the sibling case
      *> g2002-page-phrase-without-limit.)
      *> DERIVATION, every group's first LINE relative (PLUS 1):
      *> - PH prints on (HEADING integer + integer-2 - 1) = 2
      *>   (§2002:13.16.33.3 GR5b2); without HEADING it would be 1
      *>   (§2002:13.16.37.3 GR3a).
      *> - The first body group on a page prints on the FIRST DETAIL
      *>   integer = 3 (§2002:13.16.33.3 GR5b3); later lines add integer-2
      *>   to LINE-COUNTER (GR7b). Without FIRST DETAIL it would be 2
      *>   (GR3b).
      *> - PF prints on (FOOTING integer + integer-2) = 10 (GR5b4); without
      *>   FOOTING it would be LAST DETAIL + 1 = 9 (GR3e).
      *> - Page fit (§2002:13.16.33.3 GR4; trial sum per GR4c): a body
      *>   group fits when the
      *>   trial sum is less than or equal to its lower limit: LAST CONTROL
      *>   HEADING 5 for a control heading, LAST DETAIL 8 for a detail
      *>   (§2002:13.16.55.3 GR8d, GR8e). A page advance prints PF, then PH
      *>   on the new page (§2002:14.8.15.3 GR6); PF is the last group on
      *>   each page, the last one included (§2002:13.16.55.3 GR6f).
      *> GENERATE 1 (CTL A): PH line 2; CH-A lines 3,4 (printed by
      *>   §2002:14.8.15.3 GR4c; as the first body group since INITIATE
      *>   it takes no page fit test, §2002:13.16.33.3 GR4, and starts
      *>   on FIRST DETAIL, GR5b3); D: 4+1=5 <= 8, line 5.
      *> GENERATE 2 (CTL B, control break, GR5a): CH-B trial sum 5+1+1=7
      *>   > LAST CONTROL HEADING 5 (without the phrase it would be 8 and
      *>   fit, GR3c), so PF line 10, new page, PH 2 line 2, CH-B lines
      *>   3,4, D line 5.
      *> GENERATE 3..5: D lines 6, 7, 8 (8 <= 8).
      *> GENERATE 6: 8+1=9 > LAST DETAIL 8 (without the phrase it would
      *>   be FOOTING 9 and fit, GR3d): PF line 10, new page, PH 3 line 2,
      *>   D line 3 (first body group on the page).
      *> TERMINATE: PF line 10 of page 3.
      *> The read-back numbers each physical line of a page; a form feed
      *> prints "(page)" and restarts the numbering.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PG03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G2PG03.TXT".
           SELECT CHK ASSIGN TO "G2PG03.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       01  WS-K    PIC 9     VALUE 0.
       01  CTL     PIC X     VALUE "A".
       REPORT SECTION.
       RD  R CONTROL IS CTL
           PAGE LIMIT IS 10 LINES
           HEADING 2
           FIRST DETAIL 3
           LAST CONTROL HEADING 5
           LAST DETAIL 8
           FOOTING 9.
       01  PH-G TYPE PAGE HEADING LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "PH".
           02  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  CH-G TYPE CONTROL HEADING CTL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC XX VALUE "H=".
               03  COLUMN 3 PIC X SOURCE CTL.
           02  LINE PLUS 1.
               03  COLUMN 1 PIC XX VALUE "--".
       01  D1 TYPE DETAIL LINE PLUS 1.
           02  COLUMN 1 PIC X VALUE "D".
       01  PF-G TYPE PAGE FOOTING LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "PF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
           MOVE "B" TO CTL.
           PERFORM VARYING WS-K FROM 1 BY 1 UNTIL WS-K > 5
               GENERATE D1
           END-PERFORM.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = X"0A"
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = X"0C"
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   DISPLAY "(page)"
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
