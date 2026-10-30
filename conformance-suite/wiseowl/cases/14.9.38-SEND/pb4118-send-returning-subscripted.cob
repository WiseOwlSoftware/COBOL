      *>--- conformance-case
      *> id: pb4118-send-returning-subscripted
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEND TO SRV FROM WS-MSG RETURNING TBL(1). Expected: COBOL2024 (RETURNING prints data-name-1, a name).
      *> ISO 14.9.38.2 prints its operands (here RETURNING's data-name-1, after TO) as message-server-name-1 (Format 1; 14.9.38.3 SR2 "is the name of
      *> a message server run unit") or data-name-2 (Format 2): a NAME in both. A name carries no subscript (8.4.2.3.1:
      *> subscripts select an element of a table, which an identifier does), no reference modification (8.4.3.3.3 NOTE)
      *> and is no special register (8.4.3.1.2 Formats 10 and 11 are identifiers). The grammar parses all of them in the
      *> one dataReference position; the binder used to take any operand that is no message-tag item for a server name
      *> without asking how it was written (kb/Work PB4118).
      *>   cite.py: OK  14.9.38.2  OK  8.4.2.3.1  OK  8.4.3.3.3 5)
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4
      *> item 1; Annex A.3 item 4): declining it declines its behaviour, not the syntax rules of its statements, and
      *> the statement still reads its operands (kb/Work PB3568, PB4118).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4118N6.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "HELLO".
       01 WS-REPLY PIC X(8) VALUE "NONE".
       01 GRP.
          05 TBL   PIC X(8) OCCURS 3.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO SRV FROM WS-MSG RETURNING TBL(1)
           END-SEND.
           STOP RUN.
