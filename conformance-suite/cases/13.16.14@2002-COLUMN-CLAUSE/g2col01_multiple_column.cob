      *>--- conformance-case
      *> id: g2col01_multiple_column
      *> rules: [2002-GR-13.16.14.3-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.14.3 GR12 - "A multiple COLUMN clause is functionally
      *> equivalent to a COLUMN clause with a single operand, together with
      *> a simple OCCURS clause" whose integer is the number of operands,
      *> "except that the multiple COLUMN clause allows the printable items
      *> to be defined at unequal horizontal intervals".
      *>   cite.py: OK  §2002:13.16.14.3 GR12
      *> DERIVATION (no page heading; each LINE PLUS 1 is the next line):
      *>   L1 COLUMNS 1 5 9 PIC XX VALUE "AB": three printable items, each
      *>      "AB", leftmost at 1, 5 and 9 (GR6b), gaps spaces (GR10)
      *>      -> "AB  AB  AB".
      *>   L2 the OCCURS form of the same items: COLUMN 1 OCCURS 3 STEP 4,
      *>      each occurrence "integer-3 columns to the right of the
      *>      preceding occurrence" (§2002:13.16.36.3 GR12a) -> 1, 5, 9
      *>      -> "AB  AB  AB", identical to L1 (the equivalence).
      *>   L3 COLUMNS 1 4 10 (unequal intervals 3 and 6) with three SOURCE
      *>      operands: the entry is a repeating entry, so the operands go
      *>      to successive repeating printable items (§2002:13.16.51.3 GR4)
      *>      -> P1 at 1, Q2 at 4, R3 at 10; the last repetition's rightmost
      *>      column 11 is the horizontal counter (§2002:13.16.14.3 GR9), so
      *>      COLUMN PLUS 2 puts E at 13 (GR8) -> "P1 Q2    R3 E".
      *>   cite.py: OK  §2002:13.16.36.3 GR12a, §2002:13.16.51.3 GR4,
      *>            §2002:13.16.14.3 GR8, GR9, GR10
      *> Fails if a multiple COLUMN clause printed one item, placed the
      *> repetitions at equal intervals, did not count as a repeating entry
      *> for a multi-operand SOURCE, or left the counter at its first item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CLM01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           SYMBOLIC CHARACTERS SYM-X0A SYM-X0C SYM-X0D
               ARE 11 13 14.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "G2CLM01.TXT".
           SELECT CHK ASSIGN TO "G2CLM01.TXT".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-1    PIC X(2)  VALUE "P1".
       01  WS-2    PIC X(2)  VALUE "Q2".
       01  WS-3    PIC X(2)  VALUE "R3".
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-BYTE PIC X.
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LN   PIC 99    VALUE 0.
       01  WS-LINE PIC X(20) VALUE SPACES.
       REPORT SECTION.
       RD  R PAGE LIMIT IS 60 LINES.
       01  D1 TYPE DE.
           03  LINE PLUS 1.
               05  COLUMNS 1 5 9 PIC XX VALUE "AB".
           03  LINE PLUS 1.
               05  COLUMN 1 PIC XX VALUE "AB" OCCURS 3 STEP 4.
           03  LINE PLUS 1.
               05  COLUMNS 1 4 10 PIC XX SOURCES ARE WS-1 WS-2 WS-3.
               05  COLUMN PLUS 2 PIC X VALUE "E".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R.
           GENERATE D1.
           TERMINATE R.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END
                       MOVE CHK-REC TO WS-BYTE
                       PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           IF WS-I > 0
               PERFORM SHOW-LINE
           END-IF.
           STOP RUN.
       TAKE-BYTE.
           EVALUATE TRUE
               WHEN WS-BYTE = SYM-X0A
                   PERFORM SHOW-LINE
               WHEN WS-BYTE = SYM-X0C
                   IF WS-I > 0
                       PERFORM SHOW-LINE
                   END-IF
               WHEN WS-BYTE = SYM-X0D
                   CONTINUE
               WHEN OTHER
                   ADD 1 TO WS-I
                   MOVE WS-BYTE TO WS-LINE(WS-I:1)
           END-EVALUATE.
       SHOW-LINE.
           ADD 1 TO WS-LN.
           DISPLAY "LINE " WS-LN " [" WS-LINE(1:15) "]".
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
