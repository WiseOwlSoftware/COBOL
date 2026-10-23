      *>--- conformance-case
      *> id: pb3568-receive-undefined-in-exception-phrase
      *> rules: [GR-14.9.31.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The imperative statement of an ON EXCEPTION phrase is source that must compile even when the declined
      *> facility never runs it: DISPLAY NOSUCH-MSG names nothing. Expected: COBOLNET1639.
      *> ISO §14.9.31.4 GR5 a) - "If the ON EXCEPTION phrase is specified in the RECEIVE statement, control is
      *> transferred to imperative-statement-1." - a statement the standard makes reachable is a statement.
      *>   cite.py: OK  §14.9.31.4 5)  (General rules)
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOLNET1578, docs/CONFORMANCE.md
      *> section 4 item 1; Annex A.3 item 4, section 4.2.6). Declining a facility declines its behaviour, not the
      *> identifiers its statements name: the statement binds to a no-op, and every operand that is an
      *> identifier is still resolved (kb/Work PB3568). The program is otherwise legal; the facility exists at 2023.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568R2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG PIC X(4).
       01 WS-BUF PIC X(4).
       01 WS-LEN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM WS-TAG GIVING WS-BUF WS-LEN
               ON EXCEPTION DISPLAY NOSUCH-MSG
           END-RECEIVE.
           STOP RUN.
