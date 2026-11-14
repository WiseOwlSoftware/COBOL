      *>--- conformance-case
      *> id: pb5486-release-from-function-cites-1985
      *> rules: [1985-SR-XI.4.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5486: 1985 states the FROM function rule.
      *> §1985:XI.4.2.3 SR3 (as amended by X3.23a-1989): "If
      *> identifier-1 is a function-identifier, it must reference an
      *> alphanumeric function." RELEASE FROM the integer function
      *> LENGTH is refused COBOL1914 at 85, citing the sort-merge
      *> module's rule, not the 2023 SR2 wording.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5486S1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5486S1.SRT".
       DATA DIVISION.
       FILE SECTION.
       SD  SF.
       01  SR PIC X(4).
       PROCEDURE DIVISION.
       M0 SECTION.
       M1.
           SORT SF ON ASCENDING KEY SR
               INPUT PROCEDURE IS I0
               OUTPUT PROCEDURE IS O0.
           STOP RUN.
       I0 SECTION.
       I1.
           RELEASE SR FROM FUNCTION LENGTH("AB").
       O0 SECTION.
       O1.
           RETURN SF AT END CONTINUE END-RETURN.
