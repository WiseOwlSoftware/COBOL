      *>--- conformance-case
      *> id: pb4090-send-server-name-returning-omitted
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO SRVNAME FROM WS-MSG END-SEND with no RETURNING phrase. Expected: COBOL1850.
      *> ISO 14.9.38.2 Format 1 (to-message-server) prints "SEND TO { literal-1 / message-server-name-1 } FROM
      *> identifier-1" and then "RETURNING data-name-1" underlined and WITHOUT brackets (PDF page 756, printed folio
      *> 726); Format 2 prints "TO data-name-2" and no RETURNING. SRVNAME is declared nowhere and is no
      *> message-tag data item (14.9.38.3 SR4), so it is message-server-name-1 (SR2) and the statement is Format 1,
      *> whose RETURNING phrase is required: only brackets make a portion omissible (5.2.6.2).
      *>   cite.py: OK  14.9.38.2  OK  14.9.38.3 2)  OK  5.2.6.2 (Brackets)
      *> Before kb/Work PB4090 only a literal-1 operand was known to be Format 1, so this statement compiled.
      *> SEND is the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1):
      *> declining it declines its behaviour, not the syntax rules of its statements.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4090N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG PIC X(8) VALUE "MSG".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO SRVNAME FROM WS-MSG
           END-SEND.
           STOP RUN.
