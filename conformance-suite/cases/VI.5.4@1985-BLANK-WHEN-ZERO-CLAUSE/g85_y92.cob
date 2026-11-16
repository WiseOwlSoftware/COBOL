      *>--- conformance-case
      *> id: g85_y92
      *> rules: [1985-GR-VI.5.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.4.4 GR2 (cite.py --check OK): "When the BLANK WHEN ZERO
      *> clause is used for an item whose PICTURE is numeric, the category
      *> of the item is considered to be numeric edited." Numeric edited
      *> is class alphanumeric (§1985:IV, the class/category table), so a
      *> relation between WP (PIC 9(3) BLANK WHEN ZERO) and a numeric
      *> operand is NOT a numeric comparison: §1985:VI.6.3.1.1.2 (cite.py
      *> --check OK) "If one of the operands is specified as numeric, it
      *> must be an integer data item or an integer literal" and it is
      *> treated as moved to an alphanumeric item of its own size, then
      *> compared character by character, the shorter operand extended
      *> on the right with spaces. WN, the same PICTURE without the
      *> clause, is the numeric control. No operand is zero, so the
      *> blanking rule (GR1) plays no part.
      *> DERIVATION (WP and WN both hold 12, WP's image "012"):
      *>   WP = 12 : "012" vs "12" extended to "12 " -> unequal -> WP-NE
      *>   WN = 12 : algebraic 12 = 12                       -> WN-EQ
      *>   WP > 5  : "012" vs "5  ", '0' < '5'              -> WP-NOT-GT
      *>   WN > 5  : algebraic                               -> WN-GT
      *>   WP = WN : WN moved to 3 characters "012"         -> WP-WN-EQ
      *> A compiler that kept WP numeric prints WP-EQ and WP-GT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Y92.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WP  PIC 9(3) BLANK WHEN ZERO.
       01  WN  PIC 9(3).
       PROCEDURE DIVISION.
       P1.
           MOVE 12 TO WP.
           MOVE 12 TO WN.
           DISPLAY "WP=[" WP "]".
           IF WP = 12 DISPLAY "WP-EQ" ELSE DISPLAY "WP-NE".
           IF WN = 12 DISPLAY "WN-EQ" ELSE DISPLAY "WN-NE".
           IF WP > 5 DISPLAY "WP-GT" ELSE DISPLAY "WP-NOT-GT".
           IF WN > 5 DISPLAY "WN-GT" ELSE DISPLAY "WN-NOT-GT".
           IF WP = WN DISPLAY "WP-WN-EQ" ELSE DISPLAY "WP-WN-NE".
           STOP RUN.
