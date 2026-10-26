      *>--- conformance-case
      *> id: pb3568-send-unknown-raising-name
      *> rules: [GR-14.9.38.4-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO WS-TAG FROM WS-MSG RAISING EXCEPTION EC-NOSUCH - exception-name-1 is a WRITTEN exception-name, and
      *> EC-NOSUCH is none of section 14.6.13.1 (and no EC-USER- name). Expected: COBOL0711, the diagnostic every
      *> written exception-name gets.
      *> ISO §14.9.38.2 Format 2 prints "RAISING { EXCEPTION exception-name-1 | LAST EXCEPTION }".
      *>   cite.py: OK  §14.9.38.4 8)  (General rules)  "exception-name-1 or the name of the last exception raised"
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md
      *> section 4 item 1; Annex A.3 item 4, section 4.2.6). Declining a facility declines its behaviour, not the
      *> identifiers its statements name: the statement binds to a no-op, and every operand that is an
      *> identifier is still resolved (kb/Work PB3568). The program is otherwise legal; the facility exists at 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568S2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG PIC X(4).
       01 WS-MSG PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO WS-TAG FROM WS-MSG RAISING EXCEPTION EC-NOSUCH
           END-SEND.
           STOP RUN.
