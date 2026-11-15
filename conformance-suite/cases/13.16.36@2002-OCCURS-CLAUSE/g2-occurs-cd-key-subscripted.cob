      *>--- conformance-case
      *> id: g2-occurs-cd-key-subscripted
      *> rules: [2002-SR-13.16.36.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR5 - data-name-2 is written without the
      *> subscripting normally required. K1(1) in a record under an
      *> OUTPUT CD carries one, so the KEY phrase is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 TB OCCURS 3 ASCENDING KEY IS K1(1).
             10 K1 PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
