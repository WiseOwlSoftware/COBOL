      *>--- conformance-case
      *> id: pb5174-lowercase-currency-letter-85
      *> rules: [SR-13.18.40.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5174 - 1985:VI.5.9.3 SR3: "The lowercase letters corresponding to the uppercase letters representing
      *> the PICTURE symbols A, B, P, S, V, X, Z, CR, and DB are equivalent to their uppercase representations in a
      *> PICTURE character-string. All other lowercase letters are not equivalent to their corresponding uppercase
      *> representations." (cite.py: OK). The currency symbol is the uppercase W, so w is no PICTURE symbol at
      *> COBOL-85: refused COBOL0808 (1985:VI.5.9.3 SR2 admits only the symbols).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5174LC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "W".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-U PIC W99.
       01  WS-L PIC w99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "NOT REACHED".
           STOP RUN.
