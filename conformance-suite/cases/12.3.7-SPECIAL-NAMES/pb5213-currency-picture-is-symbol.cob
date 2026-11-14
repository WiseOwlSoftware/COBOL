      *>--- conformance-case
      *> id: pb5213-currency-picture-is-symbol
      *> rules: [FMT-12.3.7.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5213 - the CURRENCY SIGN clause's phrase is
      *> [ WITH PICTURE SYMBOL literal-8 ] (cite.py --check
      *> "§2023:12.3.7.2" "WITH PICTURE SYMBOL literal-8" OK): it
      *> prints no IS between PICTURE and SYMBOL, so PICTURE IS SYMBOL
      *> is refused at IS, at every edition that has the phrase (2002
      *> on). It fails when the lexer skips the IS after PICTURE as the
      *> PICTURE clause's optional word: the clause then compiled as
      *> WITH PICTURE SYMBOL "#" and the program displayed EUR1.50.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5213CUR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "EUR" WITH PICTURE IS SYMBOL "#".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC #9.99 VALUE 1.5.
       PROCEDURE DIVISION.
       P1.
           DISPLAY A.
           STOP RUN.
