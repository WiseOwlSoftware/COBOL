      *>--- conformance-case
      *> id: pb4090-send-message-tag-with-returning
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO WS-TAG FROM WS-MSG RETURNING WS-REPLY, WS-TAG being a message-tag data item. Expected: COBOL5488.
      *> ISO 14.9.38.3 SR4: "Data-name-1 and data-name-2 shall be the name of a message-tag data item", and
      *> 14.9.38.2 prints a message-tag data-name-2 as Format 2's (message-server-response) TO operand and
      *> RETURNING as Format 1's alone, so a TO name that IS a message-tag data item with a RETURNING phrase is a
      *> statement in neither format (5.2.1).
      *>   cite.py: OK  14.9.38.3 4)  OK  14.9.38.2  OK  5.2.1  (General)
      *> The program also draws COBOL1943 at the WS-TAG entry (USAGE MESSAGE-TAG is the data half of the declined
      *> facility, refused by name, docs/CONFORMANCE.md section 4 item 1); the SEND statement's own verdict is
      *> the one this case expects. SendFormatClassificationTests pins that a message-tag TO name draws neither
      *> COBOL1850 nor COBOL5488 when the phrases agree with it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4090N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "MSG".
       01 WS-REPLY PIC X(8) VALUE "NONE".
       01 WS-TAG   USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO WS-TAG FROM WS-MSG
               RETURNING WS-REPLY
           END-SEND.
           STOP RUN.
