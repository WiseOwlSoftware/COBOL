      *>--- conformance-case
      *> id: 1989-FMT-A_2_44_2
      *> rules: [1989-FMT-A.2.44.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.44.2 (THE UPPER-CASE FUNCTION). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UPCASE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-IN  PIC X(5) VALUE "abcDe".
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY FUNCTION UPPER-CASE ( WS-IN ).
           STOP RUN.
