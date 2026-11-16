      *>--- conformance-case
      *> id: g2-value-sr18-thru-nat
      *> rules: [2002-SR-13.16.61.2-18]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR18b - "when literal-2 is of class
      *> alphanumeric or national, and the runtime collating sequence
      *> is known, the value of literal-2 shall be less than the value
      *> of literal-3."  No locale or alphabet is named, so the
      *> national collating sequence is the native one, known at
      *> compile time; N"B" THRU N"A" does not ascend in it.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if the descending national range is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VR18N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NN PIC N.
           88 N-BAD VALUE N"B" THRU N"A".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
       END PROGRAM G2VR18N.
