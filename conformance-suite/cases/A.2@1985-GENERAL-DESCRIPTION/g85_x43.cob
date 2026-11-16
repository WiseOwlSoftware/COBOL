      *>--- conformance-case
      *> id: g85_x43
      *> rules: [1989-GR-A.2.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.3 GR3 (1989-GR-A.2.3-3). An output test, the
      *> accepted half beside the refusal of g85_x02 (which pins
      *> 1985-SR-IV.4.3.8.3.3-4): 'An integer function can be
      *> referenced where an integer operand is required and where a
      *> signed operand is allowed.' MOD requires integer arguments
      *> (§1989:A.2.28.3 AR1); INTEGER is an integer function.
      *> Derived: INTEGER (-2.5) is -3, the greatest
      *> integer not greater than -2.5 (§1989:A.2.15.4 RV1); MOD (-3 2)
      *> = -3 - (2 * INTEGER (-1.5)) = -3 - (2 * -2) = 1
      *> (§1989:A.2.28.4 RV1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X43.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T PIC S99.
       01  F PIC -99.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           COMPUTE T = FUNCTION INTEGER (-2.5).
           MOVE T TO F.
           DISPLAY "INTEGER " F.
           COMPUTE T = FUNCTION MOD (FUNCTION INTEGER (-2.5) 2).
           MOVE T TO F.
           DISPLAY "MOD " F.
           STOP RUN.
