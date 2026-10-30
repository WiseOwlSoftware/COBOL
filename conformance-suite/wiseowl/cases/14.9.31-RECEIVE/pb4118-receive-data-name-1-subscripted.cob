      *>--- conformance-case
      *> id: pb4118-receive-data-name-1-subscripted
      *> rules: [FMT-14.9.31.2, SR-14.9.31.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> RECEIVE FROM TBL(1) GIVING WS-BUF WS-LEN. Expected: COBOL2024 (data-name-1 subscripted).
      *> ISO 14.9.31.2 prints RECEIVE FROM data-name-1 GIVING identifier-1 data-name-2: data-name-1 and data-name-2 are
      *> NAMES, not identifiers, so neither carries a subscript (8.4.2.3.1) or a reference modifier (8.4.3.3.3 NOTE:
      *> "where data-name-n is used in a general format or syntax rule, then reference-modification is not permitted").
      *> identifier-1 may be either. Before kb/Work PB4118 DeclinedFacilityBinder resolved the three references without
      *> asking how they were written.
      *>   cite.py: OK  14.9.31.2  OK  8.4.2.3.1  OK  8.4.3.3.3 5)
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4
      *> item 1; Annex A.3 item 4): declining it declines its behaviour, not the syntax rules of its statements, and
      *> the statement still reads its operands (kb/Work PB3568, PB4118).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4118R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG   PIC X(8) VALUE "TAG".
       01 WS-BUF   PIC X(8) VALUE "NONE".
       01 WS-LEN   PIC 9(4) VALUE 0.
       01 GRP.
          05 TBL   PIC X(8) OCCURS 3.
          05 NTBL  PIC 9(4) OCCURS 3.
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE FROM TBL(1) GIVING WS-BUF WS-LEN
           END-RECEIVE.
           STOP RUN.
