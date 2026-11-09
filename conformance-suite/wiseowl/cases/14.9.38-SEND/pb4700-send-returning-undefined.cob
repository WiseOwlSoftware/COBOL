      *>--- conformance-case
      *> id: pb4700-send-returning-undefined
      *> rules: [FMT-14.9.38.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO "SRV" FROM WS-MSG RETURNING NOSUCH-TAG - data-name-1 (the message tag) names nothing. Expected: COBOL1639.
      *> ISO §2023:8.4.2.1: "a statement shall contain a reference that uniquely identifies that resource" - the operand is
      *> printed by the §2023:14.9.38.2 general format, so it is read like any identifier (kb/Work PB4700, PB3594).
      *>   cite.py: OK  §2023:8.4.2.1
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4700S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT      USAGE MESSAGE-TAG.
       01 WS-MSG  PIC X(8) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM WS-MSG RETURNING NOSUCH-TAG
           END-SEND.
           STOP RUN.
