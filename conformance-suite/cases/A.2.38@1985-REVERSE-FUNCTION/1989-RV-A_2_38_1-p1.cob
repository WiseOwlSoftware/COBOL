      *>--- conformance-case
      *> id: 1989-RV-A_2_38_1-p1
      *> rules: [1989-RV-A.2.38.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.38.1 p1 (THE REVERSE FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-21.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S21REV.
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
           MOVE FUNCTION REVERSE("ABC") TO X.
           IF NOT (X = "CBA")
               DISPLAY "BAD REV1" MOVE 1 TO E.
           MOVE FUNCTION REVERSE("AB C ")(1:2) TO X.
           IF NOT (X = " C")
               DISPLAY "BAD REV2" MOVE 1 TO E.
           MOVE ALL "*" TO X.
           MOVE FUNCTION REVERSE("XY") TO X(1:2).
           IF NOT (X(1:3) = "YX*")
               DISPLAY "BAD REV3" MOVE 1 TO E.
           MOVE FUNCTION REVERSE("1") TO X.
           IF E = 0 DISPLAY "S21REV OK" ELSE DISPLAY "S21REV FAIL".
           STOP RUN.
