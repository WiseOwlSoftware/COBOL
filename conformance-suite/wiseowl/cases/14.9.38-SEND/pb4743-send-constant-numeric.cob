      *>--- conformance-case
      *> id: pb4743-send-constant-numeric
      *> rules: [SR-14.9.38.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO K-NUM FROM WS-MSG RETURNING MT, K-NUM being a numeric constant-name (CONSTANT AS 5). Expected: COBOL6265.
      *> ISO 13.10.3 SR2: constant-name-1 "may be used anywhere that a format specifies a literal of the class and category
      *> of constant-name-1", and 13.10.4 GR1 makes the effect "as if literal-1 ... were written": the statement is
      *> SEND TO 5, whose literal-1 is numeric. 14.9.38.3 SR1: "Literal-1 shall be an alphanumeric or national literal."
      *>   cite.py: OK  13.10.3 2)  OK  13.10.4 1)  OK  14.9.38.3 1)
      *> SEND and RECEIVE are the asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1;
      *> Annex A.3 item 4); the statement is accepted inert, but its syntax rules are asked (kb/Work PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 K-NUM   CONSTANT AS 5.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO K-NUM FROM WS-MSG RETURNING MT
           END-SEND.
           STOP RUN.
