      *>--- conformance-case
      *> id: pb5171-special-names-out-of-order-85
      *> rules: [1985-FMT-VI.4.5.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5171. §1985:VI.4.5.2 prints the switch entries, then the
      *> ALPHABET, SYMBOLIC CHARACTERS and CLASS clauses, then CURRENCY SIGN,
      *> then DECIMAL-POINT, and §1985:IV.2.1: "Clauses must be written in
      *> the sequence given in the general formats" (cite.py: OK). The second
      *> CLASS clause, line 23, follows the CURRENCY SIGN clause of line 22,
      *> which the format prints after every CLASS clause: refused COBOL2987
      *> there. Two CLASS clauses are the format's ellipsis; only the place
      *> of the second is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5171OO.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A1 IS NATIVE
           CLASS VOWEL IS "AEIOU"
           CURRENCY SIGN IS "L"
           CLASS DIGIT IS "0" THRU "9".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ITEM PIC X VALUE "E".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF W-ITEM IS VOWEL DISPLAY "VOWEL" END-IF.
           STOP RUN.
