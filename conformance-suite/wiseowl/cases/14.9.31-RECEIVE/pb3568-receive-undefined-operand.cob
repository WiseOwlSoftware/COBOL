      *>--- conformance-case
      *> id: pb3568-receive-undefined-operand
      *> rules: [SR-14.9.31.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE FROM WS-TAG GIVING NOSUCH-BUF WS-LEN - identifier-1 names nothing. Expected: COBOL1639.
      *> ISO §2023:14.9.31.3 SR2 - "The data description entries of identifier-1 or any data items subordinate to it
      *> shall not contain the ANY LENGTH clause, ..." - identifier-1 has no data description entry at all.
      *>   cite.py: OK  §2023:14.9.31.3 2)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
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
