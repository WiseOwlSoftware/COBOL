      *>--- conformance-case
      *> id: g2002u-pic-cd-sr7-zero-repeat
      *> rules: [2002-SR-13.16.38.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR7 - a repetition factor of zero
      *> The entry sits in a record description entry written after an
      *> OUTPUT CD entry (§2002:A.1.2.2 SR9), so the rule reaches it:
      *> the integer in parentheses shall be unsigned and non-zero.
      *> The compiler must refuse the program; accepting it fails
      *> the case.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU07.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 P1 PIC 9(0).
       PROCEDURE DIVISION.
           DISPLAY "NOT REFUSED".
           STOP RUN.
