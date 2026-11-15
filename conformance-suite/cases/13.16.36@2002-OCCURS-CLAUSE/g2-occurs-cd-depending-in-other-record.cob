      *>--- conformance-case
      *> id: g2-occurs-cd-depending-in-other-record
      *> rules: [2002-SR-13.16.36.2-18]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.36.2 SR18 - data-name-1 shall not occupy a byte
      *> position from the table's first byte to the end of its record.
      *> R1 and R2 both follow one OUTPUT CD, so they implicitly
      *> redefine its record area (§2002:A.1.2.2 SR9): N, byte 1 of R2,
      *> is byte 1 of T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OCCN18.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC 9.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 R1.
          05 T PIC X OCCURS 1 TO 5 DEPENDING ON N.
       01 R2.
          05 N PIC 9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
