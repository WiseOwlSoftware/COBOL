      *>--- conformance-case
      *> id: 1989-GR-A_2_31_1-p1
      *> rules: [1989-GR-A.2.31.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.31.1 p1 (THE ORD FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Its
      *> expected outcome rests on the implementor's determination
      *> DOC-A.1-8 (the ordinal position of a native character is its
      *> code unit plus one), not on the rule alone. Placed from written
      *> and run by the PB5453 reader of lineage-reads-1985/s-21.json by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S21ORD.
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
           COMPUTE R = FUNCTION ORD(C1).
           IF NOT (R = 1)
               DISPLAY "BAD ORD1" MOVE 1 TO E.
           MOVE FUNCTION CHAR(FUNCTION ORD("Q")) TO X.
           IF NOT (X = "Q")
               DISPLAY "BAD ORD2" MOVE 1 TO E.
           COMPUTE R = FUNCTION ORD("B") - FUNCTION ORD("A").
           IF NOT (R = 1)
               DISPLAY "BAD ORD3" MOVE 1 TO E.
           COMPUTE W = FUNCTION MOD(FUNCTION ORD(C1) 5).
           IF NOT (W = 1)
               DISPLAY "BAD ORD4" MOVE 1 TO E.
           IF E = 0 DISPLAY "S21ORD OK" ELSE DISPLAY "S21ORD FAIL".
           STOP RUN.
