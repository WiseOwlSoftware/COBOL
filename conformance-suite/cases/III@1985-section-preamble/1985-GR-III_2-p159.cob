      *>--- conformance-case
      *> id: 1985-GR-III_2-p159
      *> rules: [1985-GR-III.2-p159, 1985-GR-III.2-p160]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p159; §1985:III.2 p160 ((section preamble)). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * Refuter probe for 1985-GR-III.2-p159 / p160: report lines
      * are numbered from 1 at the top of EACH page (a paged report,
      * two pages). Every physical line is shown with its number on
      * its page; a form feed starts a new page.
      * Expect: P1 L01 PH, P1 L03-L05 D, P2 L01 PH, P2 L03-L04 D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P159TP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "P159TP.TXT".
           SELECT CHK ASSIGN TO "P159TP.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-N    PIC 9     VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 6 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 5.
       01  PH1 TYPE PH LINE 1.
           03  COLUMN 1 PIC X(2) VALUE "PH".
           03  COLUMN 4 PIC 9 SOURCE PAGE-COUNTER.
       01  D1 TYPE DE LINE PLUS 1.
           03  COLUMN 1 PIC X(1) VALUE "D".
           03  COLUMN 3 PIC 9 SOURCE WS-N.
           03  COLUMN 5 PIC 99 SOURCE LINE-COUNTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           PERFORM 5 TIMES
               ADD 1 TO WS-N
               GENERATE D1
           END-PERFORM.
           TERMINATE R1.
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
           IF WS-BYTE = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
           IF WS-BYTE = SYM-X0C
               IF WS-I > 0
                   PERFORM SHOW-LINE
               END-IF
               DISPLAY "FF"
               MOVE 0 TO WS-LN
               ADD 1 TO WS-PG
           ELSE
           IF WS-BYTE NOT = SYM-X0D
               ADD 1 TO WS-I
               MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-IF
           END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "P" WS-PG " L" WS-LN " [" WS-LINE(1:8) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
