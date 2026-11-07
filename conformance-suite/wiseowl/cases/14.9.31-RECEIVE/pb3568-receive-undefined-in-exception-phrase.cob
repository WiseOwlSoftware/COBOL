      *>--- conformance-case
      *> id: pb3568-receive-undefined-in-exception-phrase
      *> rules: [GR-14.9.31.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The imperative statement of an ON EXCEPTION phrase is source that must compile even when the declined
      *> facility never runs it: DISPLAY NOSUCH-MSG names nothing. Expected: COBOL1639.
      *> ISO §2023:14.9.31.4 GR5 a) - "If the ON EXCEPTION phrase is specified in the RECEIVE statement, control is
      *> transferred to imperative-statement-1." - a statement the standard makes reachable is a statement.
      *>   cite.py: OK  §2023:14.9.31.4 5)  (General rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568R2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG USAGE MESSAGE-TAG.
       01 WS-BUF PIC X(4).
       01 WS-LEN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM WS-TAG GIVING WS-BUF WS-LEN
               ON EXCEPTION DISPLAY NOSUCH-MSG
           END-RECEIVE.
           STOP RUN.
