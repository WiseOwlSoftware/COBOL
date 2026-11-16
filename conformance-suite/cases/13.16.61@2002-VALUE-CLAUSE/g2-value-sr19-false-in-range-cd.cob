      *>--- conformance-case
      *> id: g2-value-sr19-false-in-range-cd
      *> rules: [2002-SR-13.16.61.2-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR19a - "when literal-2 is of a class other
      *> than alphanumeric or national, the value of literal-4 shall
      *> not be equal to any value in the range of any occurrence of
      *> literal-2 through literal-3, inclusive."  In a record under a
      *> CD entry (§2002:A.1.1.2), C-MID has VALUE 1 THRU 3 and the
      *> FALSE value 2, inside 1..3.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if a FALSE value inside the range is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VR19A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 C PIC 9.
              88 C-MID VALUE 1 THRU 3 WHEN SET TO FALSE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
       END PROGRAM G2VR19A.
