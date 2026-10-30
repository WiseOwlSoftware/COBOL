      *>--- conformance-case
      *> id: pb4090-send-returning-and-raising
      *> rules: [FMT-14.9.38.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO SRVNAME FROM WS-MSG RETURNING WS-REPLY RAISING LAST EXCEPTION. Expected: COBOL5488.
      *> ISO 14.9.38.2 prints RETURNING data-name-1 in Format 1 (to-message-server) alone and the RAISING phrase
      *> in Format 2 (message-server-response) alone, and 5.2.1 writes the words, phrases and operands of a
      *> statement "in the sequence given in the general format": a statement is written in ONE format, so one
      *> that carries a phrase of each is in neither.
      *>   cite.py: OK  14.9.38.2  OK  5.2.1  (General)
      *> The grammar keeps the two tails optional in one rule, so before kb/Work PB4090 this compiled.
      *> SEND is the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1);
      *> declining it declines its behaviour, not the syntax rules of its statements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4090N2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "MSG".
       01 WS-REPLY PIC X(8) VALUE "NONE".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO SRVNAME FROM WS-MSG
               RETURNING WS-REPLY
               RAISING LAST EXCEPTION
           END-SEND.
           STOP RUN.
