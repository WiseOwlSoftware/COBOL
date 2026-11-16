      *>--- conformance-case
      *> id: g2-value-sr19-false-nat
      *> rules: [2002-SR-13.16.61.2-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR19b - "when literal-2 is of class
      *> alphanumeric or national, and the runtime collating sequence
      *> is known, the value of literal-4 shall not be equal to the
      *> value of any literal-2 or any value in the range of any
      *> occurrence of literal-2 through literal-3, inclusive."  No
      *> locale or alphabet is named, so the native national sequence
      *> is known; the FALSE value N"B" lies in N"A" THRU N"C".
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if a national FALSE value inside the range is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VR19N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NN PIC N.
           88 N-MID VALUE N"A" THRU N"C" WHEN SET TO FALSE N"B".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
       END PROGRAM G2VR19N.
