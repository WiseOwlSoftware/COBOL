      *>--- conformance-case
      *> id: pb4743-receive-giving-based
      *> rules: [SR-14.9.31.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE MT GIVING BUF WS-LEN, BUF being BASED. Expected: COBOL6266.
      *> ISO 14.9.31.3 SR2: identifier-1 "shall not contain ... the BASED clause".
      *>   cite.py: OK  14.9.31.3 2)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N8.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       01 BUF     BASED PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE MT GIVING BUF WS-LEN
           END-RECEIVE.
           STOP RUN.
