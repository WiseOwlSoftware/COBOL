      *>--- conformance-case
      *> id: pb5298_collating_then_segment_limit_is
      *> rules: [1985-FMT-XVI.2.2.2, 1985-FMT-XVI.2.3.2]
      *> accept: [85]
      *> source-format: fixed
      *>---
      * The twin of pb5298_collating_then_segment_limit with the
      * optional IS written: SEQUENCE IS A SEGMENT-LIMIT IS 5 (X3.23-
      * 1985 XVI 2.2.2, XVI 2.3.2; kb/Work PB5298). Under the alphabet
      * A ("Z" "Y" "X" first) "Z" < "A" is true, so the program shows
      * ALPHABET; it fails if it is refused or the sequence is lost.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5298B.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. GENERIC.
       OBJECT-COMPUTER. GENERIC PROGRAM COLLATING SEQUENCE IS A
           SEGMENT-LIMIT IS 5.
       SPECIAL-NAMES. ALPHABET A IS "Z" "Y" "X".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P PIC X VALUE "Z".
       01 Q PIC X VALUE "A".
       PROCEDURE DIVISION.
       P1.
           IF P < Q DISPLAY "ALPHABET" ELSE DISPLAY "NATIVE".
           STOP RUN.
