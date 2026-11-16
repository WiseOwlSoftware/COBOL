      *>--- conformance-case
      *> id: g85_f30_05
      *> rules: [1985-SR-XIII.3.15.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.3 SR7 - NEXT PAGE is permitted in body groups
      *> (here a control heading and a control footing) and in a report
      *> footing report group.
      * RD R1 CONTROL IS FINAL, PAGE LIMIT 20 HEADING 1 FIRST DETAIL 3
      * LAST DETAIL 15 FOOTING 18.  CH FINAL: LINE 3 ON NEXT PAGE; DE:
      * LINE PLUS 1; CF FINAL: LINE 10 ON NEXT PAGE; RF: LINE 2 ON NEXT
      * PAGE.
      * DERIVATION: INITIATE: PAGE-COUNTER 1, LINE-COUNTER 0.
      * GENERATE 1 (GENERATE GR5): CH FINAL, body rule 3c, nothing on
      * the page, presented at line 3 (4a), LINE-COUNTER 3 (6d); DE,
      * rule 3b trial 3+1 <= 15, rule 4b second paragraph line 3+1 = 4,
      * LINE-COUNTER 4.  GENERATE 2: line 5, LINE-COUNTER 5.
      * TERMINATE (GR1): CF FINAL, rule 3c, a body group is on the page,
      * so a page advance (PAGE-COUNTER 2, LINE-COUNTER 0), line 10,
      * LINE-COUNTER 10; RF, report footing rule 3c, a page on which no
      * other group is presented: page advance (PAGE-COUNTER 3), line 2,
      * rule 4a LINE-COUNTER 2.
      * Printed lines in order: CH DE DE CF RF.
      * A leg fails if NEXT PAGE is refused in any of the three group
      * types, or a group is not moved to a new page.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3005.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3005.RPT".
           SELECT CHK ASSIGN TO "G85F3005.RPT".
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
       RD  R1 CONTROL IS FINAL
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15 FOOTING 18.
       01  CHG TYPE CONTROL HEADING FINAL
           LINE NUMBER IS 3 ON NEXT PAGE.
           05  COLUMN 1 PIC XX VALUE "CH".
       01  DTL TYPE DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC XX VALUE "DE".
       01  CFG TYPE CONTROL FOOTING FINAL
           LINE NUMBER IS 10 ON NEXT PAGE.
           05  COLUMN 1 PIC XX VALUE "CF".
       01  RFG TYPE REPORT FOOTING LINE NUMBER IS 2 ON NEXT PAGE.
           05  COLUMN 1 PIC XX VALUE "RF".
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
