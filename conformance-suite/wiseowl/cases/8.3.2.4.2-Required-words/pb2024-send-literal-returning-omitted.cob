      *>--- conformance-case
      *> id: pb2024-send-literal-returning-omitted
      *> rules: [SR-8.3.2.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO "SRV" FROM WS-MSG END-SEND with no RETURNING phrase. Expected: COBOL1850.
      *> ISO 14.9.38.2 Format 1 (to-message-server) prints "SEND TO { literal-1 / message-server-name-1 }
      *> FROM identifier-1" and then "RETURNING data-name-1" underlined, WITHOUT brackets (PDF page 756);
      *> Format 2 prints "TO data-name-2" and no RETURNING. A literal-1 operand is therefore Format 1,
      *> whose RETURNING phrase is required: ISO 8.3.2.4.2 1) "Keywords. Within each format, such words
      *> are uppercase and underlined", and only brackets make a portion omissible (ISO 5.2.6.2).
      *>   cite.py: OK 8.3.2.4.2 1) (Required words)  OK 5.2.6.2 (Brackets)
      *> Found with the END-SEND omission the grammar audit's inverse class reported (kb/Work PB2024).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2024S2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV" FROM WS-MSG
           END-SEND.
           STOP RUN.
