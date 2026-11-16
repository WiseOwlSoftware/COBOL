      *>--- conformance-case
      *> id: g2-add-sr2-cd-alnum-addend
      *> rules: [2002-SR-14.8.2.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.2 SR2 - "Identifier-1 and identifier-2 shall
      *> reference a numeric data item."  CX, identifier-1 of ADD CX
      *> TO CN, is an alphanumeric item (PIC X(4)) of a record under a
      *> CD entry (§2002:A.1.1.2).
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if an alphanumeric CD addend is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADS2A.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CX PIC X(4).
           05 CN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           ADD CX TO CN
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ADS2A.
