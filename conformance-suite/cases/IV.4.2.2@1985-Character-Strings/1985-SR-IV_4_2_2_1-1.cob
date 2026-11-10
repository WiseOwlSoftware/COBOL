      *>--- conformance-case
      *> id: 1985-SR-IV_4_2_2_1-1
      *> rules: [1985-SR-IV.4.2.2.1-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1 SR1 (Character-Strings). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the witness-drafting pass (a rule the
      *> audit never drafted) by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FNWORD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MAX        PIC 9(2) VALUE 4.
       01  MIN        PIC 9(2) VALUE 9.
       01  UPPER-CASE PIC X(5) VALUE "hello".
       01  REVERSE    PIC X(3) VALUE "ABC".
       01  INTEGER.
           02  NUMVAL PIC 9(2) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-1.
           COMPUTE NUMVAL = FUNCTION MAX (MAX MIN).
           DISPLAY NUMVAL OF INTEGER.
           COMPUTE NUMVAL = FUNCTION MIN (MAX MIN).
           DISPLAY NUMVAL.
           DISPLAY FUNCTION UPPER-CASE (UPPER-CASE).
           DISPLAY FUNCTION REVERSE (REVERSE).
           DISPLAY UPPER-CASE REVERSE.
           PERFORM MOD.
           COMPUTE NUMVAL = FUNCTION INTEGER (MAX / 3 + MIN).
           DISPLAY NUMVAL.
           STOP RUN.
       MOD.
           COMPUTE NUMVAL = FUNCTION MOD (MIN MAX) + MAX.
           DISPLAY NUMVAL.
