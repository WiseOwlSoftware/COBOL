      *>--- conformance-case
      *> id: 1985-SR-VI_3_2_1_2-1
      *> rules: [1985-SR-VI.3.2.1.2-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.3.2.1.2 SR1 (Structure). An output test: the program
      *> must translate and run at 85 and DISPLAY exactly the lines its
      *> .out holds, the outcome the rule requires. Placed from the
      *> audit's draft, confirmed by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
000100 IDENTIFICATION DIVISION.
000200 PROGRAM-ID. CMTENT.
000300 AUTHOR. ANY CHARS: "UNBALANCED (PAREN = + * / $ , ; .
000400     SECOND LINE OF THE SAME COMMENT-ENTRY, NO HYPHEN NEEDED.
000500 ENVIRONMENT DIVISION.
000600 DATA DIVISION.
000700 PROCEDURE DIVISION.
000800 P1.
000900     DISPLAY "OK".
001000     STOP RUN.
