      *>--- conformance-case
      *> id: pb4641_final_transfer_quotient_of_sdidi_operands
      *> rules: [DOC-A.1-123, GR-8.8.1.2-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4641 (docs/CONFORMANCE.md DOC-A.1-123): under NATIVE
      *> arithmetic the quotient that IS the final transfer is
      *> rounded ONCE, at the receiver (14.7.7 NOTE 1: ROUNDED applies
      *> only to the transfer; cite.py: OK 14.7.7). The quotient of two
      *> floating-point literals is formed on the 34-digit SDIDI, and
      *> its own rounding at the INTERMEDIATE ROUNDING mode was a
      *> second rounding: the quotient
      *> 9.999999999999999999999999999999999E-1 / 2.0E0 has 35
      *> significant digits, .49999999999999999999999999999999995 (an
      *> exact tie at the 35th), which the 34-digit rounding took UP
      *> to .5, so ROUNDED stored 1 and a truncating receiver .5000...
      *> where the value lies below .5. 8.8.1.3 leaves native
      *> evaluation to the implementor (cite.py: OK 8.8.1.3); CLAUDE.md
      *> rule 1 sends the latitude to GnuCOBOL, whose quotient is
      *> exact before the one store.
      *>
      *> EXPECTED VALUES, DERIVED (Python Decimal, exact):
      *> 0.9999999999999999999999999999999999 / 2
      *>   = 0.49999999999999999999999999999999995
      *>   ROUND  ROUNDED into PIC 9 (nearest away): 0
      *>   TRUNC  truncated into V9(30): .499999999999999999999999999999
      *>   NEG    the negated quotient truncated: -.499999...
      *>   EXACT  1.0E0 / 4.0E0 = .25 exactly, ROUNDED to V9(1): .3
      *>          (nearest-away on the exact tie .25; an exact
      *>          quotient must not be disturbed by the odd digit).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4641FINALQUOT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R    PIC 9.
       01 RT   PIC V9(30).
       01 RN   PIC SV9(30).
       01 RE   PIC V9.
       01 ET   PIC .9(30).
       01 EN   PIC -.9(30).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R ROUNDED
               = 9.999999999999999999999999999999999E-1 / 2.0E0
           DISPLAY "ROUND  " R
           COMPUTE RT
               = 9.999999999999999999999999999999999E-1 / 2.0E0
           MOVE RT TO ET
           DISPLAY "TRUNC  " ET
           COMPUTE RN
               = -9.999999999999999999999999999999999E-1 / 2.0E0
           MOVE RN TO EN
           DISPLAY "NEG    " EN
           COMPUTE RE ROUNDED = 1.0E0 / 4.0E0
           DISPLAY "EXACT  " RE
           STOP RUN.
