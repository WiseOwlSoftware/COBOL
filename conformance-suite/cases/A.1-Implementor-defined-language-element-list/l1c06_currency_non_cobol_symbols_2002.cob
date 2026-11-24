      *>--- conformance-case
      *> id: l1c06_currency_non_cobol_symbols_2002
      *> rules: [DOC-A.1-44, SR-12.3.7.3-20, SR-13.18.40.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Currency symbols outside the COBOL character repertoire at
      *> COBOL 2002, each written in its PICTURE exactly as its CURRENCY
      *> clause gives it. 2002 leaves no equivalence between such a
      *> symbol and another character (it has no counterpart of 2014
      *> 12.3.6.2 SR20; kb/Work PB6130), so this test writes none; the
      *> equivalence arms are l1c06_currency_non_cobol_symbols (2014,
      *> 2023). DOC-A.1-44: none of these characters is prohibited.
      *> Derivation: 1.5 edits as 1.50 under 9.99 and the currency
      *> STRING, as written in the clause, is inserted before it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. L1C06F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE SYMBOL "é"
           CURRENCY SIGN IS "OMG" WITH PICTURE SYMBOL "Ω"
           CURRENCY SIGN IS "FWE" WITH PICTURE SYMBOL "ｅ"
           CURRENCY SIGN IS "€"
           CURRENCY SIGN IS "GBP" WITH PICTURE SYMBOL "£"
           CURRENCY SIGN IS "FWZ" WITH PICTURE SYMBOL "ｚ"
           CURRENCY SIGN IS "FWD" WITH PICTURE SYMBOL "０".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-EU PIC é9.99.
       01 WS-OM PIC Ω9.99.
       01 WS-FW PIC ｅ9.99.
       01 WS-EC PIC €9.99.
       01 WS-GB PIC £9.99.
       01 WS-FZ PIC ｚ9.99.
       01 WS-F0 PIC ０9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 1.5 TO WS-EU WS-OM WS-FW WS-EC WS-GB WS-FZ WS-F0.
           DISPLAY "EU=[" WS-EU "]".
           DISPLAY "OM=[" WS-OM "]".
           DISPLAY "FW=[" WS-FW "]".
           DISPLAY "EC=[" WS-EC "]".
           DISPLAY "GB=[" WS-GB "]".
           DISPLAY "FZ=[" WS-FZ "]".
           DISPLAY "F0=[" WS-F0 "]".
           STOP RUN.
