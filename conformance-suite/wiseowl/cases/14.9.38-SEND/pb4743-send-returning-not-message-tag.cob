      *>--- conformance-case
      *> id: pb4743-send-returning-not-message-tag
      *> rules: [SR-14.9.38.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO "SRV" FROM WS-MSG RETURNING WS-LEN. Expected: COBOL6265 (data-name-1 is no message-tag item).
      *> ISO 14.9.38.3 SR4: "Data-name-1 and data-name-2 shall be the name of a message-tag data item."
      *>   cite.py: OK  14.9.38.3 4)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM WS-MSG RETURNING WS-LEN
           END-SEND.
           STOP RUN.
