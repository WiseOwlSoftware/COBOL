      *>--- conformance-case
      *> id: pb3321-byte-length-depending-2002
      *> rules: [RV-15.14.4-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3321. BYTE-LENGTH's rule for a DEPENDING ON item is
      *> in the 2002 text word for word (§2002:15.12.3 RV2, and in
      *> §2023:15.14.4 RV2), so it holds from 2002, the edition that
      *> introduces the function. G holds N = 3 occurrences of a one
      *> byte item, so the returned value is 3 (the length in bytes of
      *> argument-1 with the current value of the DEPENDING item).
      *> The leg fails if a compiler ignores the DEPENDING item and
      *> returns the 9 bytes of the maximum.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3321BL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N     PIC 9 VALUE 3.
       01 G.
          05 T  PIC X OCCURS 1 TO 9 TIMES DEPENDING ON N.
       01 R     PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION BYTE-LENGTH(G).
           DISPLAY R.
           STOP RUN.
