*>--- conformance-case
*> id: pb6341_report_rolled_value_decimal_point_comma
*> rules: [GR-13.18.54.4-6, GR-12.3.7.4-14]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB6341 - ISO 13.18.54.4 GR6 with 12.3.7.4 GR14a: a rolled total of entries whose VALUE operand is a noninteger numeric
      *> literal written with the comma under DECIMAL-POINT IS COMMA.
      *> GR6: "If data-name-1 specifies an item whose entry has a SOURCE or VALUE clause, the value added is that of the operand of the
      *>   SOURCE or VALUE clause."   cite.py: OK  13.18.54.4 6)  (General rules)
      *> 12.3.7.4 GR14a: "The character written in numeric literals to represent the decimal separator shall be the comma."
      *>   cite.py: OK  12.3.7.4 14) a)  (General rules)
      *> DERIVATION. X (PIC 9V9) holds 1,5 = 1.5 and Y (PIC 9V99) holds 2,25 = 2.25; the picture's V is an implied point, so the detail
      *> line prints the digits `15` at column 1 and `225` at column 5: `15  225`. TOT, PIC 99V99 in the final control footing, SUMs X and Y
      *> (SR4: numeric items of another report group), adding the value of each VALUE operand once when DET is processed (GR7 a):
      *> 1.5 + 2.25 = 3.75, printed `0375`.
      *> Why it can fail: the rolled-value reader reads the VALUE literal as written, so `1,5` is no number a sum counter can add (the
      *> sum is refused, or 0 or 15).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6341CMA.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "PB6341CMA.TXT".
           SELECT CHK ASSIGN TO "PB6341CMA.TXT".
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
       01  WS-LINE PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  R CONTROL FINAL.
       01  DET TYPE DE LINE PLUS 1.
           05  X COLUMN 1 PIC 9V9 VALUE 1,5.
           05  Y COLUMN 5 PIC 9V99 VALUE 2,25.
       01  TF TYPE CF FINAL LINE PLUS 1.
           05  TOT COLUMN 1 PIC 99V99 SUM X Y.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DET.
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
                   MOVE 0 TO WS-LN
               WHEN WS-BYTE = X"0D"
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:16) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
