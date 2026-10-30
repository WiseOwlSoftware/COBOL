      *>--- conformance-case
      *> id: pb4090-send-literal-with-raising
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND "SRV" FROM WS-MSG RAISING LAST EXCEPTION. Expected: COBOL5488.
      *> ISO 14.9.38.2 prints literal-1 as an operand of Format 1 (to-message-server) alone - Format 2
      *> (message-server-response) prints "TO data-name-2" - and the RAISING phrase in Format 2 alone, so a
      *> literal-1 operand with a RAISING phrase is a statement in neither format (5.2.1).
      *>   cite.py: OK  14.9.38.2  OK  5.2.1  (General)
      *> Before kb/Work PB4090 the RAISING phrase was never compared with the operand, and the statement drew only
      *> the missing-RETURNING screen of its Format 1 half.
      *> SEND is the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1);
      *> declining it declines its behaviour, not the syntax rules of its statements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4090N3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND "SRV" FROM WS-MSG
               RAISING LAST EXCEPTION
           END-SEND.
           STOP RUN.
