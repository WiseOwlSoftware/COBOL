      *>--- conformance-case
      *> id: pb4743-send-to-not-message-tag
      *> rules: [SR-14.9.38.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO WS-MSG FROM WS-MSG RAISING LAST. Expected: COBOL6265. RAISING is Format 2's evidence (14.9.38.2), so
      *> the TO name is data-name-2, which 14.9.38.3 SR4 requires to be a message-tag data item and WS-MSG is not.
      *>   cite.py: OK  14.9.38.3 4)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO WS-MSG FROM WS-MSG RAISING LAST
           END-SEND.
           STOP RUN.
