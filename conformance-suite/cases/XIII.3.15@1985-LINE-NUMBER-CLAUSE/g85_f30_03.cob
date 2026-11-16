      *>--- conformance-case
      *> id: g85_f30_03
      *> rules: [1985-SR-XIII.3.15.3-5, 1985-GR-XIII.3.15.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.3 SR5 - with the PAGE clause omitted, relative
      *> LINE NUMBER clauses are the form permitted in every group;
      *> §1985:XIII.3.15.4 GR4 - a later relative line is the previous
      *> line plus integer-2.
      * RD R1 has no PAGE clause.  Every LINE NUMBER clause is relative:
      * the detail DE has LINE PLUS 1 ("L1") and LINE PLUS 2 ("L2"),
      * the report footing RF has LINE PLUS 1 ("RF").
      * DERIVATION (presentation rules 3.10.8 and 3.10.10, the PAGE
      * clause omitted columns): INITIATE sets LINE-COUNTER 0 and
      * PAGE-COUNTER 1 (3.5.6 rule 4, 3.5.5 rule 4).  GENERATE 1: body
      * rule 4d, first line 0+1 = 1, GR4 second line 1+2 = 3, rule 6d
      * LINE-COUNTER 3.  GENERATE 2: lines 4 and 6, LINE-COUNTER 6.
      * TERMINATE: RF rule 3d, line 6+1 = 7, rule 4a LINE-COUNTER 7.
      * No page advance occurs, so PAGE-COUNTER stays 1.
      * Printed lines in order: L1 L2 L1 L2 RF.
      * A leg fails if a relative clause is refused without a PAGE
      * clause, or a line is placed at the wrong number.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3003.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3003.RPT".
           SELECT CHK ASSIGN TO "G85F3003.RPT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(30) VALUE SPACES.
       01  WS-PC   PIC 9     VALUE 0.
       01  WS-LC   PIC 999   VALUE 0.
       REPORT SECTION.
       RD  R1.
       01  DTL TYPE DETAIL.
           02  LINE NUMBER IS PLUS 1.
               03  COLUMN 1 PIC XX VALUE "L1".
           02  LINE NUMBER IS PLUS 2.
               03  COLUMN 1 PIC XX VALUE "L2".
       01  RFG TYPE REPORT FOOTING LINE PLUS 1.
           02  COLUMN 1 PIC XX VALUE "RF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DTL.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "G1 PC " WS-PC " LC " WS-LC.
           GENERATE DTL.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "G2 PC " WS-PC " LC " WS-LC.
           TERMINATE R1.
           MOVE PAGE-COUNTER TO WS-PC.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "T  PC " WS-PC " LC " WS-LC.
           CLOSE PRT.
           PERFORM READ-BACK.
           STOP RUN.
       READ-BACK.
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
           IF CHK-REC = SYM-LF
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = SYM-CR AND CHK-REC NOT = SYM-FF
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-I > 0
               DISPLAY "[" WS-LINE(1:2) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
