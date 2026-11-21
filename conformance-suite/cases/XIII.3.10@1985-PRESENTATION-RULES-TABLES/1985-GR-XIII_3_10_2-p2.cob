      *>--- conformance-case
      *> id: 1985-GR-XIII_3_10_2-p2
      *> rules: [1985-GR-XIII.3.10.2-p2, 1985-GR-XIII.3.10.3-1, 1985-GR-XIII.3.10.3-2, 1985-GR-XIII.3.10.3-3, 1985-GR-XIII.3.10.3-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.10.2 p2; §1985:XIII.3.10.3 GR1; §1985:XIII.3.10.3
      *> GR2; §1985:XIII.3.10.3 GR3; §1985:XIII.3.10.3 p2 (PRESENTATION
      *> RULES TABLES). An output test: the program must translate and
      *> run at 85 and DISPLAY exactly the lines its .out holds, the
      *> outcome the rule requires. Placed from written and run by the
      *> PB5453 reader of lineage-reads-1985/s-17.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
      * XIII.3.10.3 (1)-(3), XIII.3.10.2-p2: LINE NUMBER sequences
      * 'A R' (RH), 'R' (DE) and 'NP R' (RF) are permissible.
      * Expected (page/line/text of each non-blank line):
      *   P1 L02 RH1 / P1 L03 RH2 (RH table rule 3a: absolute line)
      *   P1 L06 DE1 (first body group, relative: FIRST DETAIL 6)
      *   P2 L04 RF1 / P2 L05 RF2 (RF table rule 3c: NEXT PAGE, a
      *   page of its own, absolute line 4, then +1)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RWSEQ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "rwseq.txt".
           SELECT CHK ASSIGN TO "rwseq.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R-1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-PG   PIC 9     VALUE 1.
       01  WS-LINE PIC X(10) VALUE SPACES.
       REPORT SECTION.
       RD  R-1 PAGE LIMIT 30 LINES HEADING 1 FIRST DETAIL 6
           LAST DETAIL 20 FOOTING 25.
       01  RH-1 TYPE RH.
           02  LINE 2.
               03  COLUMN 1 PIC X(3) VALUE "RH1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "RH2".
       01  DE-1 TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(3) VALUE "DE1".
       01  RF-1 TYPE RF.
           02  LINE 4 ON NEXT PAGE.
               03  COLUMN 1 PIC X(3) VALUE "RF1".
           02  LINE PLUS 1.
               03  COLUMN 1 PIC X(3) VALUE "RF2".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE R-1.
           GENERATE DE-1.
           TERMINATE R-1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC = SYM-FF
                   PERFORM SHOW-LINE
                   ADD 1 TO WS-PG
                   MOVE 0 TO WS-LN
               ELSE
                   IF CHK-REC NOT = SYM-CR
                       ADD 1 TO WS-I
                       MOVE CHK-REC TO WS-LINE(WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "P" WS-PG " L" WS-LN " " WS-LINE(1:3)
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
