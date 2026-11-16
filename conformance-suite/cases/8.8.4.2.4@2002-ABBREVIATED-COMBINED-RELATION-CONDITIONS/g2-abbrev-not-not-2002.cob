      *>--- conformance-case
      *> id: g2-abbrev-not-not-2002
      *> rules: [2002-SR-8.8.4.2.4.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.2.4.2 SR3 (cite.py OK): "The word NOT shall not be
      *> followed immediately by the word NOT or the words IS NOT."
      *> The NOT NOT half of the rule (pb5123-not-is-not-operator-2002
      *> holds the IS NOT half): A > B AND NOT NOT > C is refused at its
      *> IF.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ABR02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 5.
       01  B PIC 9 VALUE 9.
       01  C PIC 9 VALUE 6.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A > B AND NOT NOT > C DISPLAY "X"
           ELSE DISPLAY "Y".
           STOP RUN.
