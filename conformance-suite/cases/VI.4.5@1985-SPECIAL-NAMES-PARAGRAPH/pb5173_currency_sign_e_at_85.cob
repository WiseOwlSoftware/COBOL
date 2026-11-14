      *>--- conformance-case
      *> id: pb5173_currency_sign_e_at_85
      *> rules: [1985-GR-VI.4.5.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5173. §1985:VI.4.5.4 GR11: literal-6 "may be any
      *> character from the computer's character set except" the
      *> digits, "the uppercase letters A, B, C, D, P, R, S, V, X, Z;
      *> the lowercase letters a through z; or the space" and
      *> * + - , . ; ( ) " = /. E is not in the 1985 list (it is a
      *> picture symbol only from COBOL-2002, whose §2023:12.3.7.3
      *> SR22 excludes it), so it is the currency symbol here, in a
      *> fixed leading, a floating and a fixed trailing position. A
      *> compiler applying the 2002 list refuses the clause; one that
      *> reads E as its 2002 picture symbol refuses the PICTUREs.
      *> Expected (§1985 editing): 5 -> E05; 12.50 -> " E12.50"
      *> (floating, two digit positions); 12.50 -> 012.50E.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5173E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "E".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W1 PIC E99.
       01  W2 PIC EEE9.99.
       01  W3 PIC 9(3).99E.
       01  V  PIC 9(3)V99 VALUE 12.5.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO W1.
           MOVE V TO W2.
           MOVE V TO W3.
           DISPLAY "[" W1 "][" W2 "][" W3 "]".
           STOP RUN.
