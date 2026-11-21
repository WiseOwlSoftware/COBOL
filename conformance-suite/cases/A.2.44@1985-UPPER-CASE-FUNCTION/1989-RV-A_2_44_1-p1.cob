      *>--- conformance-case
      *> id: 1989-RV-A_2_44_1-p1
      *> rules: [1989-RV-A.2.44.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.44.1 p1 (THE UPPER-CASE FUNCTION). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-21.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S21UPC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R PIC S9(6)V9(6) VALUE 0.
       01 W PIC S9(6)V9(6) VALUE 0.
       01 X PIC X(30) VALUE SPACES.
       01 A1 PIC A(3) VALUE "DOG".
       01 A2 PIC A(3) VALUE "CAT".
       01 C1 PIC X VALUE LOW-VALUE.
       01 E PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       P-1.
           MOVE FUNCTION UPPER-CASE("abC1z") TO X.
           IF NOT (X = "ABC1Z")
               DISPLAY "BAD UPC1" MOVE 1 TO E.
           MOVE ALL "*" TO X.
           MOVE FUNCTION UPPER-CASE("ab") TO X(1:2).
           IF NOT (X(1:3) = "AB*")
               DISPLAY "BAD UPC2" MOVE 1 TO E.
           MOVE FUNCTION UPPER-CASE("xyz")(3:1) TO X.
           IF NOT (X = "Z")
               DISPLAY "BAD UPC3" MOVE 1 TO E.
           MOVE FUNCTION UPPER-CASE("1") TO X.
           IF E = 0 DISPLAY "S21UPC OK" ELSE DISPLAY "S21UPC FAIL".
           STOP RUN.
