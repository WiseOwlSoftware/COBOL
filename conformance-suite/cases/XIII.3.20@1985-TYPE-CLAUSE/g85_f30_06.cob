      *>--- conformance-case
      *> id: g85_f30_06
      *> rules: [1985-SR-XIII.3.20.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.20.3 SR3 - PAGE HEADING and PAGE FOOTING report
      *> groups are permitted when the RD has a PAGE clause.
      * PAGE LIMIT 20 HEADING 1 FIRST DETAIL 3 LAST DETAIL 15
      * FOOTING 18.
      * PH: LINE 1; DE: LINE PLUS 1; PF: LINE 19.
      * DERIVATION: GENERATE 1 (GENERATE GR5b) processes PH: page
      * heading table 2 row 'A R', line 1 (3a), LINE-COUNTER 1 (4a).
      * DE: body
      * rule 3b, no body group on the page, presented on this page;
      * rule 4b third paragraph, LINE-COUNTER 1 < FIRST DETAIL 3, so
      * line 3; LINE-COUNTER 3.  GENERATE 2: trial 3+1 <= 15, line 4,
      * LINE-COUNTER 4.  TERMINATE: PF is the last group on each page
      * (TYPE GR7): page footing rule 3a line 19, rule 5c LINE-COUNTER
      * 19; no page advance, PAGE-COUNTER 1.
      * Printed lines in order: PH DE DE PF.
      * A leg fails if PH or PF is refused with a PAGE clause present,
      * or either group is not presented.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3006.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3006.RPT".
           SELECT CHK ASSIGN TO "G85F3006.RPT".
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
       RD  R1
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 3
           LAST DETAIL 15 FOOTING 18.
       01  PHG TYPE PAGE HEADING LINE NUMBER IS 1.
           05  COLUMN 1 PIC XX VALUE "PH".
       01  DTL TYPE DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC XX VALUE "DE".
       01  PFG TYPE PAGE FOOTING LINE NUMBER IS 19.
           05  COLUMN 1 PIC XX VALUE "PF".
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
