      *>--- conformance-case
      *> id: pb4743-receive-tag-not-message-tag
      *> rules: [SR-14.9.31.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE WS-MSG GIVING WS-MSG WS-LEN. Expected: COBOL6265 (data-name-1 is no message-tag item).
      *> ISO 14.9.31.3 SR1: "Data-name-1 shall be the name of a message-tag data item."
      *>   cite.py: OK  14.9.31.3 1)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1;
      *> Annex A.3 item 4); the statement is accepted inert, but its syntax rules are asked (kb/Work PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE WS-MSG GIVING WS-MSG WS-LEN
           END-RECEIVE.
           STOP RUN.
