      *>--- conformance-case
      *> id: pb4743-receive-length-not-integer
      *> rules: [SR-14.9.31.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE MT GIVING WS-MSG WS-FRAC, WS-FRAC being PIC 9V99. Expected: COBOL6265 (data-name-2 is no integer).
      *> ISO 14.9.31.3 SR3: "Data-name-2 shall be an integer data item of the class numeric."
      *>   cite.py: OK  14.9.31.3 3)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1;
      *> Annex A.3 item 4); the statement is accepted inert, but its syntax rules are asked (kb/Work PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       01 WS-FRAC PIC 9V99 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE MT GIVING WS-MSG WS-FRAC
           END-RECEIVE.
           STOP RUN.
