      *>--- conformance-case
      *> id: pb5796_range_figurative_high_value_refused
      *> rules: [1985-SR-VI.6.13.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.13.3 SR4 - "Two operands connected by a THROUGH
      *> phrase must be of the same class." (cite.py --check OK.)
      *> HIGH-VALUE is a character, hence a nonnumeric literal of
      *> category alphanumeric (§1985:IV.4.2.2.2.3, IV.4.2.2.2.1.3
      *> GR3), so 1 THRU HIGH-VALUE joins a numeric and an
      *> alphanumeric operand: the figurative on the HIGH side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5796H.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9(3) VALUE 7.
       PROCEDURE DIVISION.
       P1.
           EVALUATE N
               WHEN 1 THRU HIGH-VALUE
                   DISPLAY "IN"
               WHEN OTHER
                   DISPLAY "OUT"
           END-EVALUATE.
           STOP RUN.
