      *>--- conformance-case
      *> id: l1c06_currency_non_cobol_equivalence_85
      *> rules: [SR-13.18.40.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A currency symbol that is not a COBOL character, at COBOL-85,
      *> written in the PICTURE exactly as the CURRENCY SIGN clause
      *> gives it. 1985 VI.4.5.4 GR11: literal-6 "is used in the
      *> PICTURE clause to represent the currency symbol", and it
      *> "may be any character from the computer's character set
      *> except one of the following" (the list does not hold 'é').
      *> cite.py --check "§1985:VI.4.5.4 GR11" -> OK.
      *> 1985 gives no equivalence between such a symbol and any other
      *> character: "All other lowercase letters are not equivalent
      *> to their corresponding uppercase representations"
      *> (1985 VI.5.9.3 SR3), so this test writes only the symbol as
      *> given. The equivalence latitude of 'é' with 'É' is 2014 text
      *> (2014 12.3.6.2 SR20; DOC-A.1-43, band 2014; kb/Work PB6130),
      *> and its witness is l1c06_currency_non_cobol_symbols.
      *> Derivation: PIC é9.99 holding 1.5 edits as 1.50 with the
      *> currency string "é" inserted before it: é1.50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06D.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "é".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-LO PIC é9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO WS-LO.
           DISPLAY "LO=[" WS-LO "]".
           STOP RUN.
