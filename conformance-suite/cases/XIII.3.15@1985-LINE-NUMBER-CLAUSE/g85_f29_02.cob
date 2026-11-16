      *>--- conformance-case
      *> id: g85_f29_02
      *> rules: [1985-GR-XIII.3.15.4-3, 1985-GR-XIII.3.15.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.3.15.4 GR3 - "An absolute line number specifies
      *> the line number on which the print line is presented."
      *> §1985:XIII.3.15.4 GR4 - a relative LINE NUMBER clause that is
      *> not the first in its group adds integer-2 to the line of the
      *> previous print line; "If integer-2 is zero, the line will be
      *> printed on the same line as the previous print line"; a first
      *> relative clause is placed by the presentation rules tables.
      *> PAGE LIMIT 30, HEADING 1, FIRST DETAIL 4, LAST DETAIL 20,
      *> FOOTING 25 (§1985:XIII.3.8.4 GR1). LINE-COUNTER starts at 0
      *> (§1985:XIII.4.4.4 GR1b).
      *> DERIVATION.
      *>  GENERATE DE-R (first clause relative, PLUS 3): no body group
      *>   on the page and LINE-COUNTER 0 < FIRST DETAIL 4, so line 4
      *>   (§1985:XIII.3.10.8 GR4b, third paragraph). LC = 4 (GR6d).
      *>  GENERATE DE-A: LC 4 < 5 (fit test 3a); GR3 puts ABS5 on 5
      *>   and ABS8 on 8; GR4: PLUS 2 -> 8 + 2 = 10 (REL2); PLUS 0 ->
      *>   SAME on line 10 too, at column 20. LC = 10.
      *>  GENERATE DE-R: a body group is on the page, LC 10 >= 4, so
      *>   10 + 3 = 13 (GR4b, second paragraph). LC = 13.
      *> The read-back shows every non-blank physical line with its
      *> number; a form feed is skipped (its use is not stated).
      *> Each leg fails if an absolute line is taken as relative, a
      *> PLUS 0 line advances, or a first relative line ignores the
      *> FIRST DETAIL line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2902.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-LF SYM-CR SYM-FF
               ARE 11 14 13.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g85f2902.txt".
           SELECT CHK ASSIGN TO "g85f2902.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS RPT.
       FD  CHK.
       01  CHK-REC         PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF          PIC X     VALUE "N".
       01  WS-I            PIC 99    VALUE 0.
       01  WS-LN           PIC 99    VALUE 0.
       01  WS-LC           PIC 99    VALUE 0.
       01  WS-LINE         PIC X(30) VALUE SPACES.
       REPORT SECTION.
       RD  RPT PAGE LIMIT IS 30 LINES HEADING 1 FIRST DETAIL 4
           LAST DETAIL 20 FOOTING 25.
       01  DE-R TYPE DE LINE NUMBER IS PLUS 3.
           02  COLUMN 1 PIC X(2) VALUE "R3".
       01  DE-A TYPE DE.
           02  LINE NUMBER IS 5.
               03  COLUMN 1 PIC X(4) VALUE "ABS5".
           02  LINE 8.
               03  COLUMN 1 PIC X(4) VALUE "ABS8".
           02  LINE PLUS 2.
               03  COLUMN 1 PIC X(4) VALUE "REL2".
           02  LINE PLUS 0.
               03  COLUMN 20 PIC X(4) VALUE "SAME".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT PRT.
           INITIATE RPT.
           GENERATE DE-R.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE DE-A.
           MOVE LINE-COUNTER TO WS-LC.
           DISPLAY "LC " WS-LC.
           GENERATE DE-R.
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
               DISPLAY "LINE " WS-LN " [" WS-LINE(1:23) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
