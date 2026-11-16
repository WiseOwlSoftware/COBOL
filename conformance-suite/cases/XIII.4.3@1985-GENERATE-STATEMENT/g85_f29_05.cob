      *>--- conformance-case
      *> id: g85_f29_05
      *> rules: [1985-GR-XIII.4.3.4-2, 1985-GR-XIII.4.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.3.4 GR5 - the chronologically first GENERATE
      *>   processes, in order: a. the report heading, b. the page
      *>   heading, c. "ALL control heading report groups are
      *>   processed from major to minor", d. the designated detail.
      *>   Pinned by the first five USE lines: U-RH, U-PH, U-CHF,
      *>   U-CHK, U-DB (a later GENERATE runs none of a-c again).
      *> §1985:XIII.4.3.4 GR2 - "Normally, the execution of a GENERATE
      *>   data-name-1 statement causes the report writer control
      *>   system to present the designated detail report group."
      *>   Two detail groups; each GENERATE runs the USE procedure of
      *>   the group it names, and the read-back shows that group's
      *>   line, not the other's.
      *> DERIVATION. PAGE LIMIT 20 HEADING 1 FIRST DETAIL 5 LAST
      *> DETAIL 15 FOOTING 18. RH is LINE 1 and PH LINE 2 (absolute,
      *> §1985:XIII.3.15.4 GR3), CHF LINE 5 and CHK LINE 6 (absolute,
      *> fit test 3a of §1985:XIII.3.10.8: LC < integer). Then:
      *>   GENERATE DE-B (LINE PLUS 2): a body group is on the page,
      *>     LC 6 + 2 = line 8 (GR4b of §1985:XIII.3.10.8). LC 8.
      *>   GENERATE DE-A (LINE PLUS 1): 8 + 1 = line 9. LC 9.
      *>   GENERATE DE-B: 9 + 2 = line 11. LC 11.
      *> No footing is defined, so TERMINATE presents nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2905.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85f2905.txt".
           SELECT CHK ASSIGN TO "g85f2905.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       FD  CHK.
       01  CHK-REC         PIC X.
       WORKING-STORAGE SECTION.
       01  WS-K            PIC X     VALUE "1".
       01  WS-LC           PIC 99    VALUE 0.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-I            PIC 99    VALUE 0.
       01  WS-LN           PIC 99    VALUE 0.
       01  WS-LINE         PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  RPT CONTROLS ARE FINAL WS-K
           PAGE LIMIT IS 20 LINES HEADING 1 FIRST DETAIL 5
           LAST DETAIL 15 FOOTING 18.
       01  RH-G TYPE RH LINE 1.
           02  COLUMN 1 PIC X(2) VALUE "RH".
       01  PH-G TYPE PH LINE 2.
           02  COLUMN 1 PIC X(2) VALUE "PH".
       01  CHF-G TYPE CH FINAL LINE 5.
           02  COLUMN 1 PIC X(3) VALUE "CHF".
       01  CHK-G TYPE CH WS-K LINE 6.
           02  COLUMN 1 PIC X(3) VALUE "CHK".
       01  DE-A TYPE DE LINE PLUS 1.
           02  COLUMN 1 PIC X(4) VALUE "DE-A".
       01  DE-B TYPE DE LINE PLUS 2.
           02  COLUMN 1 PIC X(4) VALUE "DE-B".
       PROCEDURE DIVISION.
       DECLARATIVES.
       U-RH SECTION.
           USE BEFORE REPORTING RH-G.
       U-RH-P.
           DISPLAY "U-RH".
       U-PH SECTION.
           USE BEFORE REPORTING PH-G.
       U-PH-P.
           DISPLAY "U-PH".
       U-CHF SECTION.
           USE BEFORE REPORTING CHF-G.
       U-CHF-P.
           DISPLAY "U-CHF".
       U-CHK SECTION.
           USE BEFORE REPORTING CHK-G.
       U-CHK-P.
           DISPLAY "U-CHK".
       U-DA SECTION.
           USE BEFORE REPORTING DE-A.
       U-DA-P.
           DISPLAY "U-DA".
       U-DB SECTION.
           USE BEFORE REPORTING DE-B.
       U-DB-P.
           DISPLAY "U-DB".
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           GENERATE DE-B.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE DE-A.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE DE-B.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           TERMINATE RPT.
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
               IF CHK-REC NOT = SYM-CR AND CHK-REC NOT = SYM-FF
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           IF WS-LINE NOT = SPACES
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:4) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
