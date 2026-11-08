      *>--- conformance-case
      *> id: pb3568-send-undefined-from-operand
      *> rules: [SR-14.9.38.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND "SRV" FROM NOSUCH-OUT - identifier-1 names nothing. Expected: COBOL1639.
      *> ISO §2023:14.9.38.3 SR3 - "The data description entries of identifier-1 or any data items subordinate to it
      *> shall not contain the ANY LENGTH clause, ..." - identifier-1 has no data description entry at all.
      *>   cite.py: OK  §2023:14.9.38.3 3)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
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
