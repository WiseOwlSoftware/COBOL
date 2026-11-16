      *>--- conformance-case
      *> id: g2-value-sr18-thru-cd
      *> rules: [2002-SR-13.16.61.2-18]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR18a - "when literal-2 is of a class other
      *> than alphanumeric or national, the value of literal-2 shall be
      *> less than the value of literal-3."  C-BAD is a condition-name
      *> of an item in a record under a CD entry (§2002:A.1.1.2) whose
      *> range 5 THRU 1 does not ascend.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if the descending range is accepted in the
      *> communication section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VR18A.
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
              88 C-BAD VALUE 5 THRU 1.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
       END PROGRAM G2VR18A.
