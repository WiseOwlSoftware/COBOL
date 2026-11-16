      *>--- conformance-case
      *> id: g85_f30_04
      *> rules: [1985-SR-XIII.3.15.3-6, 1985-GR-XIII.3.15.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.3 SR6 - one NEXT PAGE phrase, in the first
      *> LINE NUMBER clause of the group, is the permitted form;
      *> §1985:XIII.3.15.4 GR5 - the group is presented beginning on the
      *> indicated line number on a new page.
      * PAGE LIMIT 20 HEADING 1 FIRST DETAIL 3 LAST DETAIL 15
      * FOOTING 18.
      * Detail DE: LINE 4 ON NEXT PAGE ("N1"), LINE 5 ("N2"), LINE
      * PLUS 1 ("N3"): the 'NP R' sequence of 3.10.3, no NEXT GROUP.
      * DERIVATION (body table 3: fit test 3c, first line 4a, final 6d):
      * INITIATE: PAGE-COUNTER 1, LINE-COUNTER 0.  GENERATE 1: no body
      * group yet on the page and no saved next group integer, so it is
      * presented on this page at lines 4, 5, 6 (GR4); LINE-COUNTER 6.
      * GENERATE 2: a body group is on the page, so 3c advances the
      * page (PAGE-COUNTER 2, LINE-COUNTER 0, 3.5.5 rule 5 and 3.5.6
      * rule 4) and presents it at lines 4, 5, 6; LINE-COUNTER 6.
      * A leg fails if the form is refused, or the second GENERATE does
      * not start a new page at line 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3004.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3004.RPT".
           SELECT CHK ASSIGN TO "G85F3004.RPT".
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
       01  DTL TYPE DETAIL.
           02  LINE NUMBER IS 4 ON NEXT PAGE.
               03  COLUMN 1 PIC XX VALUE "N1".
           02  LINE NUMBER IS 5.
               03  COLUMN 1 PIC XX VALUE "N2".
           02  LINE NUMBER IS PLUS 1.
               03  COLUMN 1 PIC XX VALUE "N3".
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
