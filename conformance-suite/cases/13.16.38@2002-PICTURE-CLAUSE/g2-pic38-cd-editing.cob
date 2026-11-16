      *>--- conformance-case
      *> id: g2-pic38-cd-editing
      *> rules: [2002-GR-13.16.38.4-1, 2002-GR-13.16.38.4-2, 2002-GR-13.16.38.4-3, 2002-GR-13.16.38.4-4, 2002-GR-13.16.38.4-6, 2002-GR-13.16.38.4-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Every edited item below is a record description entry under an
      *> OUTPUT CD, which implicitly redefines the CD's record area
      *> (§2002:A.1.2.2 SR9). The communication facility is obsolete at
      *> 2002 (COBOL0903 warning); the warning does not fail the case.
      *> §2002:13.16.38.4 GR1 - one item per editing type: simple
      *>   insertion (CM 9,999 / AE / NE), special insertion (the '.' of
      *>   E1), fixed insertion (FX $999.99CR), floating insertion
      *>   (PA ++++.++, CA $$$$.$$, MA ----.--), zero suppression with
      *>   spaces (ZA, ZL) and with asterisks (SA).
      *> §2002:13.16.38.4 GR2 (Table 8) - alphanumeric-edited AE and
      *>   national-edited NE take simple insertion only; the floating-
      *>   point edited FP (+9.99E+99) takes fixed '+' and special '.' in
      *>   the significand and no editing in the exponent.
      *> §2002:13.16.38.4 GR3 - B 0 / , appear at the positions their
      *>   symbols hold (AE "AB CD/E0F", NE "AB CD0E/F", CM "1,234").
      *> §2002:13.16.38.4 GR4 - '.' appears at its own position ("1.50",
      *>   "***.**").
      *> §2002:13.16.38.4 GR6 - floating insertion: all-positions form
      *>   (b) with zero gives all spaces; nonzero behaves as form (a):
      *>   one sign/currency just before the first nonzero digit or the
      *>   decimal point (Table 10: '-' gives space for positive); the
      *>   second floating symbol bounds the digits so 12345 truncates to
      *>   345 ($$$9, +++9); an embedded '/' is part of the string and
      *>   takes the floating character (PS "  +123").
      *> §2002:13.16.38.4 GR7 - zero suppression: all-Z zero gives all
      *>   spaces (ZA, SL with an embedded '/'), all-'*' zero gives
      *>   asterisks with the decimal separator kept (SA "***.**"),
      *>   leading-Z form (a) keeps the fraction (ZL "   .00").
      *> Each leg fails if the editing type is applied at the wrong
      *> position, the zero-value form is wrong, or truncation differs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PIC38B.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 PA  PIC ++++.++.
           05 CA  PIC $$$$.$$.
           05 MA  PIC ----.--.
           05 SA  PIC ***.**.
           05 ZA  PIC ZZZ.ZZ.
           05 ZL  PIC ZZZ.99.
           05 E1  PIC 9.99.
           05 CT  PIC $$$9.
           05 PT  PIC +++9.
           05 SL  PIC ZZ/ZZ.
           05 PS  PIC ++/++9.
           05 CM  PIC 9,999.
           05 FX  PIC $999.99CR.
           05 AE  PIC XXBXX/X0X.
           05 NE  PIC NNBNN0N/N.
           05 FP  PIC +9.99E+99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 0 TO PA CA MA SA ZA ZL SL
           DISPLAY "Z0 PA=[" PA "] CA=[" CA "] MA=[" MA "]"
           DISPLAY "Z0 SA=[" SA "] ZA=[" ZA "] ZL=[" ZL "] SL=[" SL "]"
           MOVE 1.5 TO PA CA MA SA ZA ZL E1
           DISPLAY "V1 PA=[" PA "] CA=[" CA "] MA=[" MA "]"
           DISPLAY "V1 SA=[" SA "] ZA=[" ZA "] ZL=[" ZL "] E1=[" E1 "]"
           MOVE -0.05 TO PA MA
           DISPLAY "NEG PA=[" PA "] MA=[" MA "]"
           MOVE 12345 TO CT PT
           DISPLAY "TRUNC CT=[" CT "] PT=[" PT "]"
           MOVE 12 TO SL
           MOVE 123 TO PS
           DISPLAY "EMB SL=[" SL "] PS=[" PS "]"
           MOVE 1234 TO CM
           MOVE -12.3 TO FX
           DISPLAY "SIMPLE CM=[" CM "] FIXED FX=[" FX "]"
           MOVE "ABCDEFG" TO AE
           DISPLAY "AE=[" AE "]"
           MOVE N"ABCDEFG" TO NE
           DISPLAY "NE=[" NE "]"
           MOVE 123 TO FP
           DISPLAY "FP=[" FP "]"
           STOP RUN.
