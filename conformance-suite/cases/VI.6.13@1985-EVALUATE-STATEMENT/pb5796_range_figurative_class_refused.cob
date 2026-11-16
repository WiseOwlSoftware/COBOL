      *>--- conformance-case
      *> id: pb5796_range_figurative_class_refused
      *> rules: [1985-SR-VI.6.13.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.3 SR4 (THE EVALUATE STATEMENT) - "Two operands
      *> connected by a THROUGH phrase must be of the same class."
      *>   cite.py --check "§1985:VI.6.13.3 SR4" "Two operands connected
      *>   by a THROUGH phrase must be of the same class" -> OK
      *> SPACE represents a character of the computer's character set
      *> (§1985:IV.4.2.2.2.3), so it is a nonnumeric literal, of
      *> category alphanumeric (§1985:IV.4.2.2.2.1.3 GR3); only ZERO
      *> takes the class of the selection subject (§1985:VI.6.13.4
      *> GR1b). SPACE THRU 9 therefore joins an alphanumeric and a
      *> numeric operand. Leg: the one WHEN line; the program is
      *> otherwise legal, and pb5796_range_figurative_class_accepted
      *> is the same shape with legal operands and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5796R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       P1.
           EVALUATE N
               WHEN SPACE THRU 9
                   DISPLAY "IN"
               WHEN OTHER
                   DISPLAY "OUT"
           END-EVALUATE.
           STOP RUN.
