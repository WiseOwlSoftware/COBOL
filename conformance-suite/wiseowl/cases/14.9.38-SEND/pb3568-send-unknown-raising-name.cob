      *>--- conformance-case
      *> id: pb3568-send-unknown-raising-name
      *> rules: [GR-14.9.38.4-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO WS-TAG FROM WS-MSG RAISING EXCEPTION EC-NOSUCH - exception-name-1 is a WRITTEN exception-name, and
      *> EC-NOSUCH is none of section 14.6.13.1 (and no EC-USER- name). Expected: COBOL0711, the diagnostic every
      *> written exception-name gets.
      *> ISO §2023:14.9.38.2 Format 2 prints "RAISING { EXCEPTION exception-name-1 | LAST EXCEPTION }".
      *>   cite.py: OK  §2023:14.9.38.4 8)  (General rules)  "exception-name-1 or the name of the last exception raised"
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568S2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG USAGE MESSAGE-TAG.
       01 WS-MSG PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO WS-TAG FROM WS-MSG RAISING EXCEPTION EC-NOSUCH
           END-SEND.
           STOP RUN.
