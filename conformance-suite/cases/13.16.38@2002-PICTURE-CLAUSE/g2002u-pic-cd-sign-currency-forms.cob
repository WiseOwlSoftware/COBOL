      *>--- conformance-case
      *> id: g2002u-pic-cd-sign-currency-forms
      *> rules: [2002-SR-13.16.38.2-8, 2002-SR-13.16.38.2-9, 2002-SR-13.16.38.2-10, 2002-SR-13.16.38.2-19, 2002-SR-13.16.38.2-20, 2002-SR-13.16.38.2-21, 2002-SR-13.16.38.2-22, 2002-SR-13.16.38.2-23]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The PICTURE syntax rules of §2002:13.16.38.2 reach a record
      *> description entry written after an OUTPUT CD entry
      *> (§2002:A.1.2.2 SR9). Each item writes a form those rules
      *> leave legal; the program must compile (refusing any item
      *> fails the case) and each item must edit as 13.16.38.4 says.
      *> No CURRENCY SIGN clause: the currency symbol is '$'.
      *> E1-E3 §2002:13.16.38.2 SR8 - a string ending in ',' or '.'
      *>       as the last clause, followed by the separator period
      *>       (E2 after a USAGE clause): 12 edits to "012," "012.".
      *> F1-F3 §2002:13.16.38.2 SR9 - '++', '--', '$$': two of one
      *>       floating symbol and no A N X Z 1 9 *; one digit
      *>       position each (§2002:13.16.38.4 GR6, Table 10).
      *> G1-G5 §2002:13.16.38.2 SR10 - S, V, '.', CR, DB, E once each.
      *> H1    §2002:13.16.38.2 SR19 - the floating-point exception:
      *>       '-' in the significand with the exponent's '+'; zero
      *>       edits to " 0.0E+00" (§2002:13.16.38.4 GR8, Table 9).
      *> J1/J2 §2002:13.16.38.2 SR21 - '+' leftmost, '-' rightmost.
      *> G3/G4 §2002:13.16.38.2 SR22 - 'CR' and 'DB' rightmost.
      *> C1-C8 §2002:13.16.38.2 SR23 - the NOTE's six strings $999+
      *>       +999$ +$999 999$+ +$$99 $$99+, and 999$CR, -$999.
      *>       C1-C4, C7, C8 also pin SR20: one fixed currency symbol
      *>       with one editing sign control symbol.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PU02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WE1   PIC -9.9.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS CDDC
           TEXT LENGTH IS CDTL
           STATUS KEY IS CDSK
           ERROR KEY IS CDEK
           SYMBOLIC DESTINATION IS CDSD.
       01  CDREC.
           05 E1 PIC 999,.
           05 E2 USAGE DISPLAY PIC 999,.
           05 E3 PIC 999..
           05 F1 PIC ++.
           05 F2 PIC --.
           05 F3 PIC $$.
           05 G1 PIC S9V9.
           05 G2 PIC 99.99.
           05 G3 PIC 99CR.
           05 G4 PIC 99DB.
           05 G5 PIC 9.9E+99.
           05 H1 PIC -9.9E+99.
           05 J1 PIC +99.
           05 J2 PIC 99-.
           05 C1 PIC $999+.
           05 C2 PIC +999$.
           05 C3 PIC +$999.
           05 C4 PIC 999$+.
           05 C5 PIC +$$99.
           05 C6 PIC $$99+.
           05 C7 PIC 999$CR.
           05 C8 PIC -$999.
       PROCEDURE DIVISION.
           MOVE 12 TO E1 E2 E3
           DISPLAY "SR8 [" E1 "][" E2 "][" E3 "]"
           MOVE 5 TO F1 F2 F3
           DISPLAY "SR9 POS [" F1 "][" F2 "][" F3 "]"
           MOVE -5 TO F1 F2
           DISPLAY "SR9 NEG [" F1 "][" F2 "]"
           MOVE -1.5 TO G1
           MOVE G1 TO WE1
           MOVE 1.5 TO G2
           MOVE -5 TO G3 G4
           MOVE 0 TO G5
           DISPLAY "SR10 [" WE1 "][" G2 "][" G3 "][" G4 "][" G5 "]"
           MOVE 0 TO H1
           DISPLAY "SR19 [" H1 "]"
           MOVE -5 TO J1 J2
           DISPLAY "SR21 NEG [" J1 "][" J2 "]"
           MOVE 5 TO J1 J2
           DISPLAY "SR21 POS [" J1 "][" J2 "]"
           MOVE 12 TO C1 C2 C3 C4 C5 C6 C7 C8
           DISPLAY "SR23 POS [" C1 "][" C2 "][" C3 "][" C4 "]["
               C5 "][" C6 "][" C7 "][" C8 "]"
           MOVE -12 TO C1 C2 C3 C4 C5 C6 C7 C8
           DISPLAY "SR23 NEG [" C1 "][" C2 "][" C3 "][" C4 "]["
               C5 "][" C6 "][" C7 "][" C8 "]"
           STOP RUN.
