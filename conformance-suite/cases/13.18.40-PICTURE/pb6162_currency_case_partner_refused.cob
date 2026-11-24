      *>--- conformance-case
      *> id: pb6162_currency_case_partner_refused
      *> rules: [SR-13.18.40.3-2, SR-13.18.40.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6162. Under CURRENCY SIGN IS "é", the PICTURE
      *> É9.99 is refused at COBOL-85 and COBOL 2002: 'É' is neither
      *> the currency symbol nor a PICTURE symbol, because neither
      *> edition makes it equivalent to 'é'. 1985: "All other
      *> lowercase letters are not equivalent to their corresponding
      *> uppercase representations" (§1985:VI.5.9.3 SR3); 2002 has no
      *> counterpart of 2014's grant (§2014:12.3.6.2 SR20). From 2014
      *> the implementor's equivalence (DOC-A.1-43) admits it
      *> (l1c06_currency_non_cobol_symbols).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6162R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "é".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-UP PIC É9.99.
       PROCEDURE DIVISION.
       MAIN-P.
           STOP RUN.
