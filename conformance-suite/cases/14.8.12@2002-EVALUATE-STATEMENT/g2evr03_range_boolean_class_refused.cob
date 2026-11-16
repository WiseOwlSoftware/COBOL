      *>--- conformance-case
      *> id: g2evr03_range_boolean_class_refused
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
      *> Leg: class BOOLEAN. WS-LO and WS-HI are PIC 1(2) boolean
      *> items, class boolean; a range with boolean ends is refused.
      *> The subject is alphanumeric so that no rule about a boolean
      *> SUBJECT is in play: the one diagnosis is SR4 on the operands.
      *> Every other line is legal 2002 source, so the one diagnosis is
      *> SR4 at the WHEN line; g2evr00_range_same_class_accepted is the
      *> same shape with legal operands and runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVR03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S  PIC X(2) VALUE "01".
       01 WS-LO PIC 1(2) VALUE B"00".
       01 WS-HI PIC 1(2) VALUE B"11".
       PROCEDURE DIVISION.
       MAIN-P.
           EVALUATE WS-S
               WHEN WS-LO THRU WS-HI
                   DISPLAY "IN"
               WHEN OTHER
                   DISPLAY "OUT"
           END-EVALUATE.
           STOP RUN.
