      *>--- conformance-case
      *> id: g2-add-sr6-cd-level66
      *> rules: [2002-SR-14.8.2.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.2 SR6 - identifier-4 and identifier-5 "shall not
      *> be described with level-number 66."  CRN is a level-66 RENAMES
      *> of CG THRU CQ in a record under a CD entry (§2002:A.1.1.2), the
      *> identifier-5 of ADD CORRESPONDING.
      *> A prohibiting syntax rule: the program shall be refused.
      *> Fails if a level-66 CD operand is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADS6R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WG.
           05 A PIC 9(3) VALUE 5.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CG.
              10 A PIC 9(3).
           05 CQ PIC 9(4).
       66  CRN RENAMES CG THRU CQ.
       PROCEDURE DIVISION.
       MAIN-P.
           ADD CORRESPONDING WG TO CRN
           DISPLAY "NOT REFUSED"
           STOP RUN.
       END PROGRAM G2ADS6R.
