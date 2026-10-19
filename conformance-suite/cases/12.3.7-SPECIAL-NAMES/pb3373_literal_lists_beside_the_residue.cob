      *>--- conformance-case
      *> id: pb3373_literal_lists_beside_the_residue
      *> rules: [FMT-12.3.5.2, FMT-12.3.6.2, FMT-12.3.7.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3373 - the error production of a closed clause list now opens at a literal as it
      *> does at a word, so a literal LIST that a clause of the list owns (an ALPHABET or CLASS clause's
      *> literals, a level-88 entry's VALUES) could also be read as the clause's end and a foreign run.
      *> The reading that keeps the literals in their clause wins, so this LEGAL program means what it
      *> says.  Under ALPHABET AL-REV, "Z" "Y" and then "X" THRU "A" (ISO 12.3.7.4 GR7 k) 5: the
      *> consecutive native characters, "in either ascending or descending sequence"), B collates
      *> AFTER Y, so B > Y is true; the CLASS HEXDIG set holds 0 to 9 and A to F of the native set
      *> (GR12), so B is in it; B is no vowel and is one of A B C.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3373PS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC-BOX.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE IS AL-REV.
       SPECIAL-NAMES.
           ALPHABET AL-REV IS "Z" "Y" "X" THRU "A"
           CLASS HEXDIG IS "0" THRU "9" "A" THRU "F".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-C PIC X VALUE "B".
          88 W-VOWEL VALUES "A" "E" "I" "O" "U".
          88 W-EARLY VALUES "A" "B" "C".
       01 W-Y PIC X VALUE "Y".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF W-VOWEL
               DISPLAY "P3373PS VOWEL"
           ELSE
               DISPLAY "P3373PS CONSONANT"
           END-IF.
           IF W-EARLY
               DISPLAY "P3373PS EARLY"
           ELSE
               DISPLAY "P3373PS LATE"
           END-IF.
           IF W-C IS HEXDIG
               DISPLAY "P3373PS HEX"
           ELSE
               DISPLAY "P3373PS NOT-HEX"
           END-IF.
           IF W-C > W-Y
               DISPLAY "P3373PS REVERSED"
           ELSE
               DISPLAY "P3373PS NATIVE"
           END-IF.
           STOP RUN.
