      *>--- conformance-case
      *> id: pb5173_currency_sign_lowercase_refused_at_85
      *> rules: [1985-GR-VI.4.5.4-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5173. §1985:VI.4.5.4 GR11 excludes "the lowercase
      *> letters a through z" from literal-6, so CURRENCY SIGN IS "m"
      *> is refused at 85 (COBOL0891, line 18). From COBOL-2002 only
      *> the lowercase equivalents of A B C D E N P R S V X Z are
      *> excluded (§2023:12.3.7.3 SR22), and "m" is legal there. A
      *> compiler applying the 2002 list at 85 accepts the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5173M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           CURRENCY SIGN IS "m".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC m99.
       PROCEDURE DIVISION.
       P1.
           MOVE 5 TO W.
           DISPLAY "[" W "]".
           STOP RUN.
