      *>--- conformance-case
      *> id: pb5735-inspect-national-group-every-operand-position
      *> rules: [SR-14.9.22.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5735 - §2023:14.9.22.3 SR2: "Identifier-3, ... ,
      *>   identifier-n shall reference an elementary item described
      *>   implicitly or explicitly as usage display or national"
      *>   (§2002:14.8.21.2 SR2 likewise) and §2023:13.18.29.4 GR2b: "a
      *>   national group is treated as though it were an elementary data
      *>   item of usage national" (§2002:13.16.27.3 GR2b). So a GROUP-USAGE
      *>   NATIONAL group is a legal identifier-3 ... identifier-n in
      *>   every position: the REPLACING pattern and replacement
      *>   (ALL FROMG BY TOG: AB becomes XY), the CONVERTING pair (A to X,
      *>   B to Y), a BEFORE delimiter (CHARACTERS BEFORE INITIAL STOPG:
      *>   ABCABC has 2 characters before the first C) and an AFTER
      *>   delimiter (FIRST N"B" BY N"Q" AFTER INITIAL FROMG: the first B
      *>   after the first AB, in CABC, is the fifth character).
      *>   Expected: XYCXYC, XYCXYC, 02, ABCAQC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5735E.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X PIC N(6) VALUE N"ABCABC".
       01 Y PIC N(6) VALUE N"ABCABC".
       01 Z PIC N(6) VALUE N"ABCABC".
       01 FROMG GROUP-USAGE NATIONAL.
          05 F1 PIC N VALUE N"A".
          05 F2 PIC N VALUE N"B".
       01 TOG GROUP-USAGE NATIONAL.
          05 T1 PIC N VALUE N"X".
          05 T2 PIC N VALUE N"Y".
       01 STOPG GROUP-USAGE NATIONAL.
          05 S1 PIC N VALUE N"C".
       01 C1 PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT X REPLACING ALL FROMG BY TOG
           DISPLAY FUNCTION DISPLAY-OF (X)
           INSPECT Y CONVERTING FROMG TO TOG
           DISPLAY FUNCTION DISPLAY-OF (Y)
           INSPECT Z TALLYING C1 FOR CHARACTERS BEFORE INITIAL STOPG
           DISPLAY C1
           INSPECT Z REPLACING FIRST N"B" BY N"Q" AFTER INITIAL FROMG
           DISPLAY FUNCTION DISPLAY-OF (Z)
           STOP RUN.
