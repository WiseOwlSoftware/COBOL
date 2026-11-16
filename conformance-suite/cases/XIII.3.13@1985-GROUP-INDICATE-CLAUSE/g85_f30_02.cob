      *>--- conformance-case
      *> id: g85_f30_02
      *> rules: [1985-FMT-XIII.3.13.2, 1985-GR-XIII.3.13.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.13.2 - the format GROUP INDICATE, only GROUP
      *> underlined (rendered page 624), so INDICATE is optional;
      *> §1985:XIII.3.13.4 GR1 - the item is presented only on the first
      *> presentation in the report and after every control break.
      * Both spellings of the format are written in one detail group:
      * COLUMN 1 SOURCE K1 GROUP INDICATE and COLUMN 4 VALUE "HDR"
      * GROUP.
      * COLUMN 8 SOURCE V carries no clause.  RD CONTROL IS K1, no PAGE.
      * DERIVATION (GR1): GENERATE 1 (K1=AA,V=1) is the first
      * presentation in the report (a) => [AA HDR 1]; GENERATE 2 (V=2)
      * follows no break, so both indicated items are spaces
      * => [       2]; GENERATE 3 (K1=BB,V=3) follows a control break
      * (c) => [BB HDR 3]; GENERATE 4 (V=4) => [       4].
      * A leg fails if either spelling is refused (the format grants
      * both) or if the indicated items are presented on the wrong
      * detail.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3002.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G85F3002.RPT".
           SELECT CHK ASSIGN TO "G85F3002.RPT".
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
       01  K1      PIC XX    VALUE "AA".
       01  V       PIC 9     VALUE 1.
       REPORT SECTION.
       RD  R1 CONTROL IS K1.
       01  DE1 TYPE DETAIL LINE NUMBER IS PLUS 1.
           05  COLUMN 1 PIC XX   SOURCE K1 GROUP INDICATE.
           05  COLUMN 4 PIC X(3) VALUE "HDR" GROUP.
           05  COLUMN 8 PIC 9    SOURCE V.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE DE1.
           MOVE 2 TO V.
           GENERATE DE1.
           MOVE "BB" TO K1.
           MOVE 3 TO V.
           GENERATE DE1.
           MOVE 4 TO V.
           GENERATE DE1.
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
               DISPLAY "[" WS-LINE(1:8) "]"
               MOVE SPACES TO WS-LINE
               MOVE 0 TO WS-I
           END-IF.
