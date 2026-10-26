      *>--- conformance-case
      *> id: pb3568-receive-undefined-operand
      *> rules: [SR-14.9.31.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE FROM WS-TAG GIVING NOSUCH-BUF WS-LEN - identifier-1 names nothing. Expected: COBOL1639.
      *> ISO §14.9.31.3 SR2 - "The data description entries of identifier-1 or any data items subordinate to it
      *> shall not contain the ANY LENGTH clause, ..." - identifier-1 has no data description entry at all.
      *>   cite.py: OK  §14.9.31.3 2)  (Syntax rules)
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md
      *> section 4 item 1; Annex A.3 item 4, section 4.2.6). Declining a facility declines its behaviour, not the
      *> identifiers its statements name: the statement binds to a no-op, and every operand that is an
      *> identifier is still resolved (kb/Work PB3568). The program is otherwise legal; the facility exists at 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG PIC X(4).
       01 WS-LEN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM WS-TAG GIVING NOSUCH-BUF WS-LEN
           END-RECEIVE.
           STOP RUN.
