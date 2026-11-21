      *>--- conformance-case
      *> id: 1985-GR-III_2-p7
      *> rules: [1985-GR-III.2-p7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:III.2 p7 ((section preamble)). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/g-01.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * 1985-GR-III.2-p7: an alphanumeric function's value is a string
      * of one or more characters. Expect: F1 ABC 3 OK / F2 B 1 OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P7AF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S   PIC X(3).
       01  L   PIC 9.
       PROCEDURE DIVISION.
       P0.
           MOVE FUNCTION UPPER-CASE ("abc") TO S.
           COMPUTE L = FUNCTION LENGTH (FUNCTION UPPER-CASE ("abc")).
           IF S = "ABC" AND L = 3
               DISPLAY "F1 " S " " L " OK"
           ELSE
               DISPLAY "F1 " S " " L " BAD"
           END-IF.
           COMPUTE L = FUNCTION LENGTH (FUNCTION CHAR (67)).
           IF FUNCTION CHAR (67) = "B" AND L = 1
               DISPLAY "F2 " FUNCTION CHAR (67) " " L " OK"
           ELSE
               DISPLAY "F2 " FUNCTION CHAR (67) " " L " BAD"
           END-IF.
           STOP RUN.
