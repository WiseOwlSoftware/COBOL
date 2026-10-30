      *>--- conformance-case
      *> id: pb2024-send-end-send-omitted
      *> rules: [SR-8.3.2.4.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO SRVNAME FROM WS-MSG RETURNING WS-REPLY with no END-SEND. Expected: COBOL1850.
      *> ISO 14.9.38.2 prints END-SEND underlined, on a line of its own, WITHOUT brackets, in both
      *> formats (PDF page 756, printed folio 726), while it brackets the exception phrases above it.
      *> ISO 8.3.2.4.2 1) - required words: "Keywords. Within each format, such words are uppercase
      *> and underlined."  ISO 5.2.2 - keywords "are required in order to select the functionality
      *> associated with that keyword, subject to the conventions specified in 5.2.6"; ISO 5.2.6.2 -
      *> only brackets make "that portion of the general format" omissible.
      *>   cite.py: OK 8.3.2.4.2 1) (Required words)  OK 5.2.2 (Keywords)  OK 5.2.6.2 (Brackets)
      *> The grammar's END_SEND? let the omission compile clean (kb/Work PB2024, found by the inverse
      *> class of scripts/spec/audit_grammar_optional_words.py). SEND is the declined messaging facility
      *> (COBOL1578), which declines the behaviour, never the syntax rules; the program is otherwise legal
      *> at 2023, the facility's edition: Format 1 (to-message-server), whose TO name SRVNAME is a
      *> message-server-name-1 (14.9.38.3 SR2) and whose RETURNING data-name-1 line is written (kb/Work PB4090),
      *> so END-SEND is the only phrase missing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2024S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REPLY PIC X(8) VALUE "NONE".
       01 WS-MSG PIC X(8) VALUE "MSG".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO SRVNAME FROM WS-MSG RETURNING WS-REPLY.
           STOP RUN.
