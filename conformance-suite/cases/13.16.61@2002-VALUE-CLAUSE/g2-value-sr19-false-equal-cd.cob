      *>--- conformance-case
      *> id: g2-value-sr19-false-equal-cd
      *> rules: [2002-SR-13.16.61.2-19]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR19 - "The value of literal-4 shall not be
      *> equal to the value of any occurrence of literal-2."  In a
      *> record under a CD entry (§2002:A.1.1.2), C-EQ has VALUES 3 4
      *> and the FALSE value 4, equal to its second literal-2.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if a FALSE value equal to a later literal-2 is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VR19B.
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
              88 C-EQ VALUES 3 4 WHEN SET TO FALSE 4.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NOT REFUSED".
           STOP RUN.
       END PROGRAM G2VR19B.
