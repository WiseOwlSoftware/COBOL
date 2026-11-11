      *>--- conformance-case
      *> id: 1985-GR-VI_6_19_4-4-all-literal
      *> rules: [1985-GR-VI.6.19.4-4]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> kb/Work PB5176. §1985:IV.4.2.2.2.3: a figurative constant
      *> associated with a data item is repeated "character by character
      *> on the right until the size of the resultant string is greater
      *> than or equal to the number of character positions in the
      *> associated data item", and §1985:VI.6.19.4 GR3 puts it in
      *> category alphanumeric, so "data is moved as if the sending
      *> operand were described as an unsigned numeric integer"
      *> (§1985:VI.6.19.4 GR4), aligned by decimal point with any
      *> necessary zero filling and then edited (§1985:VI.6.19.4 GR4
      *> b). Expected, by the rules, the integer of the repeated string
      *> and the receiver's own alignment and editing:
      *>   ALL "5"  to 9(3)V99 (5 positions): 55555    -> 555.00
      *>   ALL "12" to 9(3)V99:               12121    -> 121.00
      *>   ALL "5"  to ZZ9.99 (6 positions):  555555   -> 555.00
      *>   ALL "12" to 9(3).99 (6 positions): 121212   -> 212.00
      *>   ALL "12" to S9(3) SIGN LEADING SEPARATE (4 positions, the
      *>            sign is one): 1212 -> the three low-order digits,
      *>            +212, which moved to PIC X(3) is 212 (the sign is
      *>            not moved, §1985:VI.6.19.4 GR4 a)
      *> A compiler that fills the digit positions of the noninteger
      *> receiver by position gives 555.55 and 121.21, one that stores
      *> the repeated characters as the edited item's image gives
      *> 555555, and one that counts three positions for the separate
      *> sign item gives +121. The numeric edited receivers are shown
      *> as stored; the numeric ones through an edited item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MVALLN.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N3V2 PIC 9(3)V99.
       01  NE   PIC ZZ9.99.
       01  NP   PIC 9(3).99.
       01  NSS  PIC S9(3) SIGN LEADING SEPARATE.
       01  SHOW PIC ZZ9.99.
       01  SX   PIC X(3).
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "5" TO N3V2.
           MOVE N3V2 TO SHOW.
           DISPLAY "A [" SHOW "]".
           MOVE ALL "12" TO N3V2.
           MOVE N3V2 TO SHOW.
           DISPLAY "B [" SHOW "]".
           MOVE ALL "5" TO NE.
           DISPLAY "C [" NE "]".
           MOVE ALL "12" TO NP.
           DISPLAY "D [" NP "]".
           MOVE ALL "12" TO NSS.
           MOVE NSS TO SX.
           DISPLAY "E [" SX "]".
           STOP RUN.
