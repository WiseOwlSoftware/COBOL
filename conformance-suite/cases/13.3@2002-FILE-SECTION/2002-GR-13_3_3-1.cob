      *>--- conformance-case
      *> id: 2002-GR-13_3_3-1
      *> rules: [2002-GR-13.3.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.3.3 GR1 - "A data-item format or table format VALUE
      *> clause specified in the file section is ignored except in the
      *> execution of the INITIALIZE statement. The initial value of a
      *> data item in the file section is undefined." The initial value
      *> is never asserted: each record is given a known content (MOVE
      *> ALL ".") before it is shown.
      *> The INITIALIZE readings are §2002:14.8.19.3 GR5c.1.b (a
      *> data-item or table format VALUE makes an item a receiver of
      *> the VALUE phrase), GR6 a) 3. (the sender is the VALUE literal,
      *> per occurrence for a table element) and GR6 c) (no phrase:
      *> SPACES for alphanumeric, ZEROES for numeric); the table VALUE
      *> places "AA" "BB" "CC" in occurrences 1, 2, 3 (§2002:13.16.61.3
      *> GR12).
      *> Legs (R is RA X(3) "ABC", RN 9(3) 123, RT X(2) OCCURS 3):
      *>   F-NUM   - INITIALIZE R NUMERIC TO VALUE: only RN takes its
      *>             VALUE, [...123......]; fails if the alphanumeric
      *>             or table VALUEs are applied too.
      *>   F-ALL   - INITIALIZE R ALL TO VALUE: data-item and table
      *>             format VALUEs both apply, [ABC123AABBCC].
      *>   F-NONE  - INITIALIZE R (no phrase): the VALUE clauses take
      *>             no part, [   000      ]; fails if a VALUE leaks.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FSV1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2fsv1.dat" ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 RA PIC X(3) VALUE "ABC".
          05 RN PIC 9(3) VALUE 123.
          05 RT PIC X(2) OCCURS 3 VALUES ARE "AA" "BB" "CC" FROM (1).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE ALL "." TO R.
           INITIALIZE R NUMERIC TO VALUE.
           DISPLAY "F-NUM=[" R "]".
           MOVE ALL "." TO R.
           INITIALIZE R ALL TO VALUE.
           DISPLAY "F-ALL=[" R "]".
           MOVE ALL "." TO R.
           INITIALIZE R.
           DISPLAY "F-NONE=[" R "]".
           CLOSE F.
           STOP RUN.
