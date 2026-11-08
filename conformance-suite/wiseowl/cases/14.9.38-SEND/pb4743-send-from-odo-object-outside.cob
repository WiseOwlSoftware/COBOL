      *>--- conformance-case
      *> id: pb4743-send-from-odo-object-outside
      *> rules: [SR-14.9.38.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO "SRV" FROM REC RETURNING MT, REC's table depending on WS-COUNT, an item OUTSIDE REC. Expected:
      *> COBOL6266. ISO 14.9.38.3 SR3: identifier-1 shall not contain "the OCCURS clause with the DEPENDING ON phrase
      *> where the depending on data item is not within identifier-1" (the message carries no count the server holds).
      *>   cite.py: OK  14.9.38.3 3)  (Syntax rules)
      *> SEND and RECEIVE are the asynchronous messaging facility (Annex A.3 item 4, docs/CONFORMANCE.md section 2): the statement binds, and every operand and syntax rule is asked at bind (kb/Work PB3568, PB4743).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       01 WS-LEN  PIC 9(4) VALUE 0.
       01 WS-COUNT PIC 9 VALUE 3.
       01 REC.
          05 ITEMS PIC X OCCURS 1 TO 5 DEPENDING ON WS-COUNT.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM REC RETURNING MT
           END-SEND.
           STOP RUN.
