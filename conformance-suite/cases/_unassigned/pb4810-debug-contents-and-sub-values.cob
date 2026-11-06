      *>--- conformance-case
      *> id: pb4810-debug-contents-and-sub-values
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING, the values the
      *> data-name trigger gives DEBUG-ITEM.
      *> General rule 10 describes DEBUG-SUB-1 to DEBUG-SUB-3 as PICTURE
      *> S9999 SIGN IS LEADING SEPARATE: a numeric item, so a comparison and
      *> COMPUTE read the occurrence number and DISPLAY shows the separate
      *> sign and four digits.
      *> Rule 24 c: DEBUG-CONTENTS holds "the content of the data item
      *> referenced by identifier-1", the whole item, whatever window of it
      *> the statement used (syntax rule 12 bars reference modification of
      *> identifier-1). Rule 11: the move is "treated exactly as if it was an
      *> alphanumeric to alphanumeric elementary move with no conversion of
      *> data from one form of internal representation to another", so a
      *> binary or packed item arrives as its own bytes.
      *> Why each leg can fail: DEBUG-SUB-1 held as characters is not equal
      *> to 3 and COMPUTE refuses it; DEBUG-CONTENTS of a reference-modified
      *> statement is the window; DEBUG-CONTENTS of a COMP item is its
      *> decimal digits, not the two bytes.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810CS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T.
           02  E    PIC X OCCURS 5 VALUE SPACE.
       01  D        PIC X(6) VALUE "ABCDEF".
       01  W6       PIC X(6).
       01  N        PIC S9(4) COMP VALUE 0.
       01  NX       REDEFINES N PIC XX.
       01  C3       PIC S9(5) COMP-3 VALUE 0.
       01  C3X      REDEFINES C3 PIC XXX.
       01  M        PIC 9(4).
       01  W2       PIC XX.
       01  W3       PIC XXX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBE SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF E.
       DBE-B.
           IF DEBUG-SUB-1 = 3
              DISPLAY "SUB EQ 3"
           ELSE
              DISPLAY "SUB NE 3".
           COMPUTE M = DEBUG-SUB-1 + 1.
           DISPLAY "M=" M.
           DISPLAY "SUB=" DEBUG-SUB-1.
       DBD SECTION.
           USE FOR DEBUGGING ON ALL REFERENCES OF D.
       DBD-B.
           MOVE DEBUG-CONTENTS TO W6.
           DISPLAY "D[" W6 "]".
       DBN SECTION.
           USE FOR DEBUGGING ON N.
       DBN-B.
           MOVE DEBUG-CONTENTS TO W2.
           IF W2 = NX DISPLAY "BINARY BYTES" ELSE DISPLAY "NOT BYTES".
       DBC SECTION.
           USE FOR DEBUGGING ON C3.
       DBC-B.
           MOVE DEBUG-CONTENTS TO W3.
           IF W3 = C3X DISPLAY "PACKED BYTES" ELSE DISPLAY "NOT BYTES".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "Z" TO E (3).
           MOVE "XY" TO D (2:2).
           MOVE D (4:2) TO W6.
           MOVE 34 TO N.
           MOVE -34 TO C3.
           STOP RUN.
