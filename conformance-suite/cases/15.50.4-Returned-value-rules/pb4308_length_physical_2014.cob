*>--- conformance-case
*> id: pb4308_length_physical_2014
*> rules: [RV-15.50.4-3, RV-15.50.4-8, RV-15.14.4-1, RV-15.14.4-7, FMT-15.50.2,
*>    FMT-15.14.2]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> PHYSICAL, THE KEYWORD OF FUNCTION LENGTH / BYTE-LENGTH, IS A 2014 INTRODUCTION (kb/Work PB4308).
      *> 15.50.2 prints  FUNCTION LENGTH ( argument-1 [ PHYSICAL ] )  and 15.14.2  FUNCTION BYTE-LENGTH
      *> ( argument-1 [ PHYSICAL ] ); the printed 2002 formats are  ( argument-1 )  alone, and below 2014 PHYSICAL is an
      *> ordinary user-defined word (reserved-words.json: r85 false, r2002 false, r2014 true). This test is the
      *> introducing edition's witness; its refusals below 2014 are pb4308-length-physical-below-2014 and
      *> pb4308-byte-length-physical-below-2014.
      *>
      *> What each line proves (WS-FIX is a FIXED-length group of 4 + 6 alphanumeric positions, so PHYSICAL changes
      *> nothing: 15.50.4 r8 and 15.14.4 r7 speak of a variable-length group only):
      *>   LENPHYS / LENPLAIN - 15.50.4 r3: the length in alphanumeric character positions = 10, with and without the keyword.
      *>   BYTPHYS / BYTPLAIN - 15.14.4 r1: the length in bytes = 10, with and without the keyword (usage DISPLAY, one byte per position).
      *>   ELEMPHYS - an elementary PIC X(4) item takes the keyword too: 4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4308PHY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-FIX.
          05 F1 PIC X(4).
          05 F2 PIC X(6).
       01 WS-ELEM PIC X(4).
       01 R PIC 9(4).
       PROCEDURE DIVISION.
           COMPUTE R = FUNCTION LENGTH(WS-FIX PHYSICAL)
           DISPLAY "LENPHYS=" R
           COMPUTE R = FUNCTION LENGTH(WS-FIX)
           DISPLAY "LENPLAIN=" R
           COMPUTE R = FUNCTION BYTE-LENGTH(WS-FIX PHYSICAL)
           DISPLAY "BYTPHYS=" R
           COMPUTE R = FUNCTION BYTE-LENGTH(WS-FIX)
           DISPLAY "BYTPLAIN=" R
           COMPUTE R = FUNCTION LENGTH(WS-ELEM PHYSICAL)
           DISPLAY "ELEMPHYS=" R
           STOP RUN.
