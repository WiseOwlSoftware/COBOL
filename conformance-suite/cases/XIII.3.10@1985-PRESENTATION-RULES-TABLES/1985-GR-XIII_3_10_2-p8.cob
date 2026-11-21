      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_2-p8
      *> rules: [1985-GR-XIII.3.10.2-p8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.2 p8 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-17 refute: 1985 first print line position rules and final
      * LINE-COUNTER setting rules on a PAGED report (XIII.3.10.6-10).
      * Expected (1985 rules): P1 L04 RH, L05 PH, L07 D1A, L08 D1B,
      * L09 D2, L16 PF; P2 L03 PH, L07 D1A, L08 D1B, L09 D3, L16 PF;
      * P3 L03 PH, L09 D1A, L10 D1B, L16 PF, L18 RF.
      * LC after GENERATEs: 08 12 08 15 10.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S17RF1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "S17RF1.TXT".
           SELECT CHK ASSIGN TO "S17RF1.TXT".
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
       01  WS-LC   PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 20 LINES HEADING 3 FIRST DETAIL 7
           LAST DETAIL 14 FOOTING 15.
       01  RH1 TYPE RH LINE PLUS 2.
           03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH1 TYPE PH LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "PH".
       01  D1 TYPE DE.
           03  LINE PLUS 2.
               05  COLUMN 1 PIC X(3) VALUE "D1A".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC X(3) VALUE "D1B".
       01  D2 TYPE DE LINE PLUS 1 NEXT GROUP 12.
           03  COLUMN 1 PIC X(2) VALUE "D2".
       01  D3 TYPE DE LINE PLUS 1 NEXT GROUP 8.
           03  COLUMN 1 PIC X(2) VALUE "D3".
       01  PF1 TYPE PF LINE 16.
           03  COLUMN 1 PIC X(2) VALUE "PF".
       01  RF1 TYPE RF LINE PLUS 2.
           03  COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE D2.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE D1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE D3.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE D1.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
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
           EVALUATE TRUE
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
                   MOVE 0 TO WS-LN
                   ADD 1 TO WS-PG
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "P" WS-PG " L" WS-LN " [" WS-LINE(1:4) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
