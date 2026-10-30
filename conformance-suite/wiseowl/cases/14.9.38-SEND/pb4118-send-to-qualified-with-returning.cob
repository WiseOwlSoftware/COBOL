      *>--- conformance-case
      *> id: pb4118-send-to-qualified-with-returning
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO FLD OF GRP-B FROM WS-MSG RETURNING WS-REPLY. Expected: COBOL5488 (a qualified TO name is Format 2's,
      *> and RETURNING is Format 1's).
      *> A message-server-name-1 is a bare name (14.9.38.3 SR2), so a TO operand written with a qualifier (OF or IN,
      *> 8.4.2.2.2 Format 1) can only be Format 2's data-name-2, which 14.9.38.2 prints without RETURNING. Format 2's
      *> data-name-2 is a data reference like any other, and 8.4.2.1 requires that "a statement shall contain a
      *> reference that uniquely identifies" the resource (kb/Work PB4118; before it the qualified name was taken for a
      *> server name and never resolved).
      *>   cite.py: OK  14.9.38.3 2)  OK  14.9.38.2  OK  8.4.2.2.2
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4
      *> item 1; Annex A.3 item 4): declining it declines its behaviour, not the syntax rules of its statements, and
      *> the statement still reads its operands (kb/Work PB3568, PB4118).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4118N5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "HELLO".
       01 WS-REPLY PIC X(8) VALUE "NONE".
       01 GRP-B.
          05 FLD   PIC X(8).
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO FLD OF GRP-B FROM WS-MSG RETURNING WS-REPLY
           END-SEND.
           STOP RUN.
