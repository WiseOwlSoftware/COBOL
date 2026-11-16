      *>--- conformance-case
      *> id: g85_bf03_01
      *> rules: [1985-GR-IV.4.2.2.2.3-3, 1985-GR-IV.4.2.2.2.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.2.3 GR3 - [ALL] HIGH-VALUE(S) is the character
      *> with the highest ordinal position in the program collating
      *> sequence; GR4 - [ALL] LOW-VALUE(S) the lowest. AL names every
      *> native character once (ordinals 1-65536), "M" first and "Q"
      *> last, so the program collating sequence (§1985:VI.4.4.4 GR5)
      *> makes LOW-VALUE "M" and HIGH-VALUE "Q": a compiler that used
      *> the native extremes, or ignored the clause, prints other
      *> characters on the LV/HV lines and fails the comparisons.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0301.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       OBJECT-COMPUTER. WISEOWL
           PROGRAM COLLATING SEQUENCE IS AL.
       SPECIAL-NAMES.
           ALPHABET AL IS "M" 1 THRU 77 79 THRU 81 83 THRU 65536
               "Q".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X3 PIC X(3).
       01  Y3 PIC X(3) VALUE ALL HIGH-VALUES.
       01  Z3 PIC X(3) VALUE LOW-VALUE.
       PROCEDURE DIVISION.
       P1.
           MOVE HIGH-VALUE TO X3.
           DISPLAY "HV=" X3.
           MOVE ALL LOW-VALUES TO X3.
           DISPLAY "LV=" X3.
           DISPLAY "HV1=" HIGH-VALUES " LV1=" LOW-VALUE.
           DISPLAY "VALUE-HV=" Y3 " VALUE-LV=" Z3.
           IF Y3 = ALL HIGH-VALUE
               DISPLAY "Y3-EQ-HV"
           ELSE
               DISPLAY "Y3-NE-HV".
           MOVE "ZZZ" TO X3.
           IF X3 < HIGH-VALUE
               DISPLAY "ZZZ-LT-HV"
           ELSE
               DISPLAY "ZZZ-NOT-LT-HV".
           MOVE "AAA" TO X3.
           IF X3 > LOW-VALUES
               DISPLAY "AAA-GT-LV"
           ELSE
               DISPLAY "AAA-NOT-GT-LV".
           STOP RUN.
