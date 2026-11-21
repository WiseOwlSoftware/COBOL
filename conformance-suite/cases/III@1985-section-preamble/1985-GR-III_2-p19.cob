      *>--- conformance-case
      *> id: 1985-GR-III_2-p19
      *> rules: [1985-GR-III.2-p19]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p19 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p19: the bottom margin is an empty area after
      * the page body. LINAGE 2 LINES AT BOTTOM 2: two records fill
      * the body, the third starts page 2; lines 3 and 4 of page 1
      * are empty. Expect: L1 X / L2 Y / L3 EMPTY / L4 EMPTY / L5 Z
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P19BM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "P19BM.TXT".
           SELECT CHK ASSIGN TO "P19BM.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT LINAGE IS 2 LINES LINES AT BOTTOM 2.
       01  PRT-REC PIC X.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(10) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           MOVE "X" TO PRT-REC.
           WRITE PRT-REC BEFORE ADVANCING 1 LINE.
           MOVE "Y" TO PRT-REC.
           WRITE PRT-REC BEFORE ADVANCING 1 LINE.
           MOVE "Z" TO PRT-REC.
           WRITE PRT-REC BEFORE ADVANCING 1 LINE.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y" OR WS-LN > 4
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0 PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC = SYM-X0C
                   DISPLAY "FORMFEED"
               ELSE
                   IF CHK-REC NOT = SYM-X0D
                       ADD 1 TO WS-I
                       MOVE CHK-REC TO WS-LINE (WS-I:1)
                   END-IF
               END-IF
           END-IF.
       SHOW-LINE.
           ADD 1 TO WS-LN
           IF WS-LINE = SPACES
               DISPLAY "L" WS-LN " EMPTY"
           ELSE
               DISPLAY "L" WS-LN " " WS-LINE (1:WS-I)
           END-IF
           MOVE SPACES TO WS-LINE
           MOVE 0 TO WS-I.
