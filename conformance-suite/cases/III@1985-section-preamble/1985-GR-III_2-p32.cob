      *>--- conformance-case
      *> id: 1985-GR-III_2-p32
      *> rules: [1985-GR-III.2-p32]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p32 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p32: print-line columns are numbered from 1 at
      * the leftmost position: COLUMN 1 and COLUMN 5 land at the
      * first and fifth characters. Expect: COL [A   B] OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P32CL.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "P32CL.TXT".
           SELECT CHK ASSIGN TO "P32CL.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-DONE PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R.
       01  DL TYPE DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X VALUE "A".
           05  COLUMN 5 PIC X VALUE "B".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE DL.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y" OR WS-DONE = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-LINE = "A   B"
               DISPLAY "COL [" WS-LINE (1:5) "] OK"
           ELSE
               DISPLAY "COL [" WS-LINE "] BAD"
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = SYM-X0A
               IF WS-I > 0
                   MOVE "Y" TO WS-DONE
               END-IF
           ELSE
               IF CHK-REC NOT = SYM-X0D AND CHK-REC NOT = SYM-X0C
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE (WS-I:1)
               END-IF
           END-IF.
