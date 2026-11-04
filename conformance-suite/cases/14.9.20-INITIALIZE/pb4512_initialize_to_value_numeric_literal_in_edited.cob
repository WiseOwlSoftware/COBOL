*>--- conformance-case
*> id: pb4512_initialize_to_value_numeric_literal_in_edited
*> rules: [GR-14.9.20.4-6]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4512NL.
      *> kb/Work PB4512 -- ISO 14.9.20.4 GR6 a) 3., the NUMERIC-literal arm, at the edition (COBOL-2023,
      *> 13.18.63.3 SR6) that admits a numeric literal as the VALUE of a numeric-edited item.
      *>   cite.py: OK  §14.9.20.4 6) a) 3.  (General rules)
      *>   cite.py: OK  §13.18.63.3 11)  (Syntax rules)
      *>   "Editing characters in a picture character-string for a numeric-edited data item are used in editing of the
      *>   initial value when the data item is initialized and the literal is numeric."
      *> The initial value of N1 PIC ZZ9.99 VALUE 12.5 is the EDITED image [ 12.50]; INITIALIZE ... TO VALUE restores
      *> exactly that, not the number's digits (a store of the unedited literal text would give [12.5  ] or similar).
      *> N2 VALUE ZERO is the edited zero [  0.00]; N3 PIC ZZZ9 BLANK WHEN ZERO VALUE 0 is blank, because the
      *> literal is numeric so BLANK WHEN ZERO applies (GR8 excludes only alphanumeric and national literals).
      *>   cite.py: OK  §13.18.63.4 8)  (General rules)
      *> Each item is first overwritten with ALL "Z", so a store that never happens shows [ZZZZZZ].
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-GRP.
          05 N1 PIC ZZ9.99 VALUE 12.5.
          05 N2 PIC ZZ9.99 VALUE ZERO.
          05 N3 PIC ZZZ9 BLANK WHEN ZERO VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "START [" N1 "][" N2 "][" N3 "]"
           MOVE ALL "Z" TO WS-GRP
           INITIALIZE WS-GRP ALL TO VALUE
           DISPLAY "INIT  [" N1 "][" N2 "][" N3 "]"
           STOP RUN.
