      *>--- conformance-case
      *> id: g2-add-sr6-elementary-id4
      *> rules: [2002-SR-14.8.2.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.2 SR6 - "Identifier-4 and identifier-5 shall be
      *> alphanumeric group items, national group items, or strongly-
      *> typed group items and shall not be described with level-number
      *> 66."
      *> WE, identifier-4 of ADD CORRESPONDING, is elementary;
      *> identifier-5 CG is a group of a record under a CD entry
      *> (§2002:A.1.1.2), so only identifier-4 violates the rule.
      *> A prohibiting syntax rule: the program shall be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADS6I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WE PIC 9(3) VALUE 5.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CG.
              10 A PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           ADD CORRESPONDING WE TO CG
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ADS6I.
