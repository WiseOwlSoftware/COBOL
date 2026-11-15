      *>--- conformance-case
      *> id: g2-occurs-cd-index-in-add
      *> rules: [2002-SR-13.16.36.2-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR7 - index-name-1 appears only as a subscript,
      *> in PERFORM or SEARCH VARYING, in SET or in a relation condition.
      *> ADD 1 TO IX uses the index of a table under an OUTPUT CD as an
      *> identifier, so it is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 REC1.
          05 T PIC X OCCURS 3 INDEXED BY IX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1 TO IX
           DISPLAY "COMPILED"
           STOP RUN.
