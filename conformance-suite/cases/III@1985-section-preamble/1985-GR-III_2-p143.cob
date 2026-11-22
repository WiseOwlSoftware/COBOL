      *>--- conformance-case
      *> id: 1985-GR-III_2-p143
      *> rules: [1985-GR-III.2-p143]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p143 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-03.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985 III.2 p143 (Amendment 1): an integer function returns a
      * numeric value with all digits right of the decimal point
      * zero, and is usable where an integer argument is required.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P143.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X PIC 9V99.
       01  W PIC X(5) VALUE "HELLO".
       01  C PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           COMPUTE X = FUNCTION INTEGER (2.75).
           DISPLAY "INTEGER " X.
           COMPUTE X = FUNCTION LENGTH (W).
           DISPLAY "LENGTH " X.
           MOVE FUNCTION CHAR (FUNCTION ORD ("A")) TO C.
           DISPLAY "CHAR-ORD " C.
           IF FUNCTION INTEGER (2.75) = 2 AND
              FUNCTION LENGTH (W) = 5 AND C = "A"
               DISPLAY "P143 OK"
           ELSE
               DISPLAY "P143 FAIL".
           STOP RUN.
