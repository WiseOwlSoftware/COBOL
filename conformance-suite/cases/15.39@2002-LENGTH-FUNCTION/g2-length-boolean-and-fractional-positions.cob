      *>--- conformance-case
      *> id: g2-length-boolean-and-fractional-positions
      *> rules: [2002-RV-15.39.3-1, 2002-RV-15.39.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.39.3 RV1 - "If argument-1 is a bit group item, an
      *> elementary boolean data item, a boolean literal, or a type
      *> declaration for a boolean item, the returned value is an
      *> integer equal to the length of argument-1 in boolean
      *> positions." One leg per kind (lines BG..TBIT):
      *>   BG   01 GROUP-USAGE BIT of 1(3) + 1(4); adjacent bits
      *>        (§2002:8.5.1.5.3), no filler at the end of a record that
      *>        is entirely a bit group                       -> 7
      *>   SBG  a subordinate bit group of 1(3) + 1(9); the filler that
      *>        follows it takes the level of the next item, 05 -> 12
      *>   BBIT elementary PIC 1(5) USAGE BIT                -> 5
      *>   BDSP elementary PIC 1(5) (usage display)          -> 5
      *>   BNAT elementary PIC 1(3) USAGE NATIONAL           -> 3
      *>   BLIT the boolean literal B"101101"                -> 6
      *>   TBIT TYPEDEF PIC 1(6) USAGE BIT                   -> 6
      *> A leg fails if the bit items are counted in alphanumeric
      *> positions (BG, SBG, BBIT, TBIT would give 1, 2, 1, 1) or the
      *> national boolean in bytes (BNAT 6).
      *> §2002:15.39.3 RV6 - "When the returned value is expressed as a
      *> number of alphanumeric character positions and argument-1 does
      *> not occupy an integral number of positions, the returned value
      *> is rounded to the next larger integer value." A group with no
      *> GROUP-USAGE clause is an alphanumeric group item
      *> (§2002:13.16.27.3 GR3), so its length is in alphanumeric
      *> positions (§2002:15.39.3 RV3). The filler after a bit item in
      *> an alphanumeric group is "of the same level number as the next
      *> item within that group" (§2002:8.5.1.5.3) - level 05 here, so
      *> it lies outside the level-05 group W-AGn, which therefore
      *> occupies only its bits:
      *>   AG10 10 bits = 1.25 positions, rounded up         -> 2
      *>   AG3   3 bits = 0.375 positions, rounded up        -> 1
      *>   AG8   8 bits, integral: the control              -> 1
      *> (truncating instead of rounding up gives AG10 1 and AG3 0).
      *> The whole records include that filler (§2002:15.39.3 RV5) and
      *> are integral: A10 = 10 + 6 + 8 bits, A3 = 3 + 5 + 16 bits:
      *>   A10 -> 3, A3 -> 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LEN01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-BIT IS TYPEDEF PIC 1(6) USAGE BIT.
       01  W-BG GROUP-USAGE BIT.
           05 W-BG1 PIC 1(3).
           05 W-BG2 PIC 1(4).
       01  W-R.
           05 W-SBG GROUP-USAGE BIT.
              10 W-SB1 PIC 1(3).
              10 W-SB2 PIC 1(9).
           05 W-RX  PIC X.
       01  W-BBIT   PIC 1(5) USAGE BIT.
       01  W-BDSP   PIC 1(5).
       01  W-BNAT   PIC 1(3) USAGE NATIONAL.
       01  W-A10.
           05 W-AG10.
              10 W-AB10 PIC 1(10) USAGE BIT.
           05 W-AX  PIC X.
       01  W-A3.
           05 W-AG3.
              10 W-AB3 PIC 1(3) USAGE BIT.
           05 W-AY  PIC X(2).
       01  W-A8.
           05 W-AG8.
              10 W-AB8 PIC 1(8) USAGE BIT.
           05 W-AZ  PIC X.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "BG=" FUNCTION LENGTH(W-BG)
           DISPLAY "SBG=" FUNCTION LENGTH(W-SBG)
           DISPLAY "BBIT=" FUNCTION LENGTH(W-BBIT)
           DISPLAY "BDSP=" FUNCTION LENGTH(W-BDSP)
           DISPLAY "BNAT=" FUNCTION LENGTH(W-BNAT)
           DISPLAY "BLIT=" FUNCTION LENGTH(B"101101")
           DISPLAY "TBIT=" FUNCTION LENGTH(T-BIT)
           DISPLAY "AG10=" FUNCTION LENGTH(W-AG10)
           DISPLAY "AG3=" FUNCTION LENGTH(W-AG3)
           DISPLAY "AG8=" FUNCTION LENGTH(W-AG8)
           DISPLAY "A10=" FUNCTION LENGTH(W-A10)
           DISPLAY "A3=" FUNCTION LENGTH(W-A3)
           STOP RUN.
