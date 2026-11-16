      *>--- conformance-case
      *> id: g2-add-sr6-cd-elementary
      *> rules: [2002-SR-14.8.2.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.2 SR6 - "Identifier-4 and identifier-5 shall be
      *> alphanumeric group items, national group items, or
      *> strongly-typed group items and shall not be described with
      *> level-number 66."  CN, identifier-5 of ADD CORRESPONDING, is an
      *> elementary item of a record under a CD entry (§2002:A.1.1.2).
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if an elementary CD operand is accepted.  The accepted
      *> CD-group leg is g2-add-corresponding-cd-group.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADS6E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WG.
           05 A PIC 9(3) VALUE 5.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CN PIC 9(4).
           05 CG.
              10 A PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-P.
           ADD CORRESPONDING WG TO CN
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ADS6E.
