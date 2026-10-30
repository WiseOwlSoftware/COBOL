      *>--- conformance-case
      *> id: l1c20_occurs_usage_per_occurrence
      *> rules: [GR-13.18.38.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §13.18.38.4 GR1 — the USAGE clause of an OCCURS entry
      *> applies to each occurrence (formats 1 and 2): the storage form
      *> is the clause's, on the LAST occurrence as on the first.
      *>
      *> THE RULES.
      *> §13.18.38.4 GR1: "Except for the OCCURS clause itself, all
      *>   data description clauses associated with an item whose
      *>   description includes an OCCURS clause apply to each
      *>   occurrence of the item described."
      *>   OK  §13.18.38.4 1)  (General rules)
      *> §13.18.38.4 GR7: "The value of the data item referenced by
      *>   data-name-1 represents the current number of occurrences of
      *>   the subject of the entry."
      *>   OK  §13.18.38.4 7)  (General rules)
      *> §13.18.38.4 GR8 a): when data-name-1 is outside the
      *>   occurs-depending group, only that part of the table area
      *>   specified by its value at the start of the operation is
      *>   used (the printed text of GR8 a), read from the spec).
      *> §14.9.25.4 GR4: "Any move that is not an elementary move, and
      *>   does not reference a variable-length group, is treated
      *>   exactly as if it were an alphanumeric to alphanumeric
      *>   elementary move"
      *>   OK  §14.9.25.4 4)  (General rules)
      *> §14.6.8.5: alphanumeric receiving data is "aligned at the
      *>   leftmost character position in the data item with space fill
      *>   or truncation to the right".
      *>   OK  §14.6.8.5   (Receiving data items of categories ...)
      *> §13.18.40.5 Table 8: '-' gives space for a positive or zero
      *>   value, '-' for a negative value.
      *> The storage the clause gives one occurrence is the
      *>   implementor's determination, docs/CONFORMANCE.md DOC-A.1-205
      *>   (BINARY: 3-4 digits is 2 bytes), DOC-A.1-215 (PACKED-DECIMAL:
      *>   digits/2 + 1 bytes, so 3 digits is 2) and DOC-A.1-209
      *>   (DISPLAY: one byte per character position).
      *>
      *> DERIVATION. Each group is moved to X PIC X(9) (preset to all
      *> nines): the group's bytes are copied and the rest of X is
      *> space filled. X(7:3) is therefore the part of X beyond the
      *> group's third occurrence, so it answers the group's size.
      *> G1 PACKED-DECIMAL 3 x 2 = 6 bytes: X(7:3) = spaces. A wrong
      *>   implementation that stored the later occurrences in DISPLAY
      *>   form would make the group 9 bytes and X(7:3) digits.
      *> G2 BINARY 3 x 2 = 6 bytes: X(7:3) = spaces.
      *> G3 DISPLAY control, 3 x 3 = 9 bytes holding 001 002 003:
      *>   X(7:3) = "003".
      *> G4 format 2, N = 3 (GR8 a): 3 x 2 = 6 bytes: X(7:3) = spaces.
      *> The arithmetic runs on the LAST occurrence, reading the first:
      *> P3 = -9 + (-5) = -14; B3 = -1 - (-300) = 299;
      *> Q3 = (-3) * 8 = -24; P2 = 7 stays 7. Each is shown through
      *> E PIC -999.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C20U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G1.
          05 P  PIC S9(3) PACKED-DECIMAL OCCURS 3 TIMES.
       01 G2.
          05 B  PIC S9(4) BINARY OCCURS 3 TIMES.
       01 G3.
          05 D  PIC 9(3) OCCURS 3 TIMES.
       01 N     PIC 9 VALUE 3.
       01 G4.
          05 Q  PIC S9(3) PACKED-DECIMAL
                OCCURS 1 TO 4 TIMES DEPENDING ON N.
       01 X     PIC X(9).
       01 E     PIC -999.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -5 TO P (1).
           MOVE 7 TO P (2).
           MOVE -9 TO P (3).
           ADD P (1) TO P (3).
           MOVE P (3) TO E.
           DISPLAY "P3=[" E "]".
           MOVE P (2) TO E.
           DISPLAY "P2=[" E "]".
           MOVE ALL "9" TO X.
           MOVE G1 TO X.
           DISPLAY "G1=[" X (7:3) "]".
           MOVE -300 TO B (1).
           MOVE 1234 TO B (2).
           MOVE -1 TO B (3).
           SUBTRACT B (1) FROM B (3).
           MOVE B (3) TO E.
           DISPLAY "B3=[" E "]".
           MOVE ALL "9" TO X.
           MOVE G2 TO X.
           DISPLAY "G2=[" X (7:3) "]".
           MOVE 1 TO D (1).
           MOVE 2 TO D (2).
           MOVE 3 TO D (3).
           MOVE ALL "9" TO X.
           MOVE G3 TO X.
           DISPLAY "G3=[" X (7:3) "]".
           MOVE 8 TO Q (1).
           MOVE 6 TO Q (2).
           MOVE -3 TO Q (3).
           MULTIPLY Q (1) BY Q (3).
           MOVE Q (3) TO E.
           DISPLAY "Q3=[" E "]".
           MOVE ALL "9" TO X.
           MOVE G4 TO X.
           DISPLAY "G4=[" X (7:3) "]".
           STOP RUN.
