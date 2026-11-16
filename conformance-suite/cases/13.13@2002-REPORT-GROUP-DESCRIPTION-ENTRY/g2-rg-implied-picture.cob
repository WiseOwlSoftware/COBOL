      *>--- conformance-case
      *> id: g2-rg-implied-picture
      *> rules: [2002-SR-13.13.2-14]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.13.2 SR14 - "The PICTURE clause may be omitted for an
      *> elementary item when an alphanumeric, boolean or national
      *> literal is specified in the VALUE clause." The implied picture
      *> is X(length), 1(length) or N(length), length the literal's
      *> length. The granted form: four report items with COLUMN and
      *> VALUE and no PICTURE, one of each literal class, are accepted.
      *> §2002:13.16.14.3 GR3: the printable-size is "the number of
      *> columns required for printing the characters described by the
      *> item's PICTURE clause"; GR8: "The position of the item's
      *> leftmost character is obtained by adding integer-2 to the
      *> current line's horizontal counter", and GR9 makes each item's
      *> rightmost column the new counter. So COLUMN PLUS 1 places an
      *> item in the column right after the item before it, and each
      *> implied length shows as where every later item starts.
      *> Every cite.py --check here is OK.
      *> DERIVATION (one detail line; counter after each item):
      *>   "ABC"   -> PIC X(3)  columns 1-3     counter 3
      *>   "DEFGH" -> PIC X(5)  columns 4-8     counter 8
      *>   B"101"  -> PIC 1(3)  columns 9-11    counter 11
      *>   N"XY"   -> PIC N(2)  columns 12-13   counter 13 (one
      *>              column per national character: the width is
      *>              the implementor's, §2002:13.16.14.3 GR2,
      *>              implementor choice DOC-A.1-160)
      *>   "Z" (PIC X written) at COLUMN PLUS 1 -> column 14
      *>   printed line "ABCDEFGH101XYZ", shown in a 20-column frame
      *>   (the frame is spaces past column 14 however the line is
      *>   padded): "[ABCDEFGH101XYZ      ]".
      *> A leg fails if an entry without PICTURE is refused, if any
      *> implied length differs from the literal's (every later item
      *> shifts), or if the boolean or national value is not printed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RGD05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PRT ASSIGN TO "g2rgd01.txt".
           SELECT CHK ASSIGN TO "g2rgd01.txt".
       DATA DIVISION.
       FILE SECTION.
       FD  PRT REPORT IS R1.
       FD  CHK.
       01  CHK-REC PIC X.
       WORKING-STORAGE SECTION.
       01  WS-EOF  PIC X     VALUE "N".
       01  WS-I    PIC 99    VALUE 0.
       01  WS-LINE PIC X(40) VALUE SPACES.
       REPORT SECTION.
       RD  R1 PAGE LIMIT IS 10 LINES.
       01  D1 TYPE DE.
           03  LINE PLUS 1.
               05  COLUMN 1 VALUE "ABC".
               05  COLUMN PLUS 1 VALUE "DEFGH".
               05  COLUMN PLUS 1 VALUE B"101".
               05  COLUMN PLUS 1 VALUE N"XY".
               05  COLUMN PLUS 1 PIC X VALUE "Z".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PRT.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE PRT.
           OPEN INPUT CHK.
           PERFORM UNTIL WS-EOF = "Y"
               READ CHK
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END PERFORM TAKE-BYTE
               END-READ
           END-PERFORM.
           CLOSE CHK.
           PERFORM SHOW-LINE.
           STOP RUN.
       TAKE-BYTE.
           IF CHK-REC = X"0A"
               PERFORM SHOW-LINE
           ELSE
               IF CHK-REC NOT = X"0D" AND CHK-REC NOT = X"0C"
                   ADD 1 TO WS-I
                   MOVE CHK-REC TO WS-LINE(WS-I:1)
               END-IF
           END-IF.
       SHOW-LINE.
           IF WS-LINE NOT = SPACES
               DISPLAY "[" WS-LINE(1:20) "]"
           END-IF.
           MOVE SPACES TO WS-LINE.
           MOVE 0 TO WS-I.
