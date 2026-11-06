      *>--- conformance-case
      *> id: pb4743-receive-continue-message-not-received
      *> rules: [FMT-14.9.31.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE MT GIVING WS-MSG WS-LEN CONTINUE AFTER MESSAGE FOO. Expected: COBOL0001 (a parse error at FOO).
      *> ISO 14.9.31.2 prints the phrase as "CONTINUE [AFTER] { arithmetic-expression-1 [SECONDS] | MESSAGE RECEIVED }":
      *> the word after MESSAGE is RECEIVED. The arm was messageKeyword cobolWord, so any word compiled.
      *>   cite.py: OK  14.9.31.2  (General format)  "MESSAGE RECEIVED"
      *> SEND and RECEIVE are the asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1;
      *> Annex A.3 item 4); the statement is accepted inert, but its syntax rules are asked (kb/Work PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N9.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE MT GIVING WS-MSG WS-LEN
               CONTINUE AFTER MESSAGE FOO
           END-RECEIVE.
           STOP RUN.
