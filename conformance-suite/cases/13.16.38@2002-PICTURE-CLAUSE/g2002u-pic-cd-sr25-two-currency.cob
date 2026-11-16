      *>--- conformance-case
      *> id: g2002u-pic-cd-sr25-two-currency
      *> rules: [2002-SR-13.16.38.2-25]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.38.2 SR25 - "For floating insertion, when the
      *> currency symbol is used as the floating insertion symbol, all
      *> occurrences shall be the same currency symbol." "UU$9" is one
      *> floating string of three currency symbols, 'U' (for "EUR"), 'U'
      *> and '$', both declared by CURRENCY SIGN clauses. SR25 is the
      *> only rule it breaks: it is one string of currency symbols
      *> (SR24), its currency symbol is leftmost (SR23), it holds two
      *> identical floating symbols (§2002:13.16.38.3 GR12), and 'U' is a
      *> permitted literal-8 (§2002:12.2.6.2 SR23). The entry, in a
      *> record under an OUTPUT CD, must be refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PSR25.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "U"
           CURRENCY SIGN IS "$".
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 A PIC UU$9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "COMPILED"
           STOP RUN.
