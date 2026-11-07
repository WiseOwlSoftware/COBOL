      *>--- conformance-case
      *> id: pb4743-send-literal-numeric
      *> rules: [SR-14.9.38.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO 5 FROM WS-MSG RETURNING MT. Expected: COBOL6265 (literal-1 is a numeric literal).
      *> ISO 14.9.38.3 SR1: "Literal-1 shall be an alphanumeric or national literal."
      *>   cite.py: OK  14.9.38.3 1)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO 5 FROM WS-MSG RETURNING MT
           END-SEND.
           STOP RUN.
