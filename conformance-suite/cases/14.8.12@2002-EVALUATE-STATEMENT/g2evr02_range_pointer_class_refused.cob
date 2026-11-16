      *>--- conformance-case
      *> id: g2evr02_range_pointer_class_refused
      *> rules: [2002-SR-14.8.12.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.12.2 SR4 - "The two operands in a range-expression
      *> shall be of the same class and shall not be of class boolean,
      *> object or pointer."
      *>   cite.py --check "§2002:14.8.12.2 SR4" "The two operands in a
      *>   range-expression shall be of the same class and shall not be
      *>   of class boolean, object or pointer" -> OK
      *> Leg: class POINTER. WS-LO and WS-HI are USAGE POINTER, class
      *> pointer; a range with pointer ends is refused.
      *> Every other line is legal 2002 source, so the one diagnosis is
      *> SR4 at the WHEN line; g2evr00_range_same_class_accepted is the
      *> same shape with legal operands and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVR02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S  USAGE POINTER.
       01 WS-LO USAGE POINTER.
       01 WS-HI USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-S
               WHEN WS-LO THRU WS-HI
                   DISPLAY "IN"
               WHEN OTHER
                   DISPLAY "OUT"
           END-EVALUATE.
           STOP RUN.
