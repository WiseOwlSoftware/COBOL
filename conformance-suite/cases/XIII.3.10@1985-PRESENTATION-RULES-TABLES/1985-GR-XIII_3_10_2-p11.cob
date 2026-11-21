      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_2-p11
      *> rules: [1985-GR-XIII.3.10.2-p11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.2 p11 (PRESENTATION RULES TABLES). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-99.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-99 g5: 1985 XIII.3.10.2 p11 final LINE-COUNTER setting
      * rules for RH (XIII.3.10.6 GR5a: absolute NEXT GROUP) and PF
      * (XIII.3.10.9 GR5b: relative NEXT GROUP).
      * Expected: RH at L02, NEXT GROUP 5 -> LC 5, PH LINE PLUS 1
      * at L06; DE LINE 9 (LC 09); PF LINE 17 NEXT GROUP PLUS 2
      * -> LC 19, RF LINE PLUS 1 at L20.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G5RHPF.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G5RHPF.TXT".
           SELECT CHK ASSIGN TO "G5RHPF.TXT".
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
       RD  R1 PAGE LIMIT IS 20 LINES HEADING 2 FIRST DETAIL 8
           LAST DETAIL 14 FOOTING 16.
       01  RH1 TYPE RH LINE 2 NEXT GROUP 5.
           03  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH1 TYPE PH LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "PH".
       01  D1 TYPE DE LINE 9.
           03  COLUMN 1 PIC X(2) VALUE "D1".
       01  PF1 TYPE PF LINE 17 NEXT GROUP PLUS 2.
           03  COLUMN 1 PIC X(2) VALUE "PF".
       01  RF1 TYPE RF LINE PLUS 1.
           03  COLUMN 1 PIC X(2) VALUE "RF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
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
