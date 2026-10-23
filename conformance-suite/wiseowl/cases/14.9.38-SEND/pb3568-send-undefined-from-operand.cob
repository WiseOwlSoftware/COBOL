      *>--- conformance-case
      *> id: pb3568-send-undefined-from-operand
      *> rules: [SR-14.9.38.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND "SRV" FROM NOSUCH-OUT - identifier-1 names nothing. Expected: COBOLNET1639.
      *> ISO §14.9.38.3 SR3 - "The data description entries of identifier-1 or any data items subordinate to it
      *> shall not contain the ANY LENGTH clause, ..." - identifier-1 has no data description entry at all.
      *>   cite.py: OK  §14.9.38.3 3)  (Syntax rules)
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOLNET1578, docs/CONFORMANCE.md
      *> section 4 item 1; Annex A.3 item 4, section 4.2.6). Declining a facility declines its behaviour, not the
      *> identifiers its statements name: the statement binds to a no-op, and every operand that is an
      *> identifier is still resolved (kb/Work PB3568). The program is otherwise legal; the facility exists at 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REPLY PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           SEND "SRV" FROM NOSUCH-OUT RETURNING WS-REPLY
           END-SEND.
           STOP RUN.
