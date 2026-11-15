      *>--- conformance-case
      *> id: g2-occurs-cd-key-sibling
      *> rules: [2002-SR-13.16.36.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR3 - the first data-name-2 names the OCCURS
      *> entry or an entry subordinate to it. SIB is a sibling of TB in
      *> a record under an OUTPUT CD, so the KEY phrase is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 SIB PIC 9.
          05 TB OCCURS 3 ASCENDING KEY IS SIB.
             10 K1 PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
