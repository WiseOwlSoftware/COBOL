      *>--- conformance-case
      *> id: pb4118-send-names-and-identifiers-accepted
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The legal forms next to the refused shapes of PB4118: a qualified data-name-2, a subscripted identifier-1.
      *> Three legs, each a statement the new screen must NOT refuse:
      *>   leg 1  SEND TO FLD OF GRP-B FROM WS-MSG - a QUALIFIED TO name is data-name-2 (Format 2), which 8.4.2.2.2 Format 1
      *>          lets carry OF/IN qualifiers; Format 2 prints no RETURNING (14.9.38.2), so none is needed;
      *>   leg 2  SEND TO SRV FROM TBL(2) RETURNING WS-REPLY - identifier-1 (FROM) is an identifier and may be subscripted;
      *>   leg 3  RECEIVE FROM WS-TAG GIVING TBL(3) WS-LEN - identifier-1 (GIVING) may be subscripted too.
      *> EXPECTED VALUES, DERIVED: the facility is declined (COBOL1578, nothing is sent or received), so the items keep
      *> their VALUE clauses and the three DISPLAYs print what the program started with.
      *>   cite.py: OK  14.9.38.2  OK  8.4.2.2.2  OK  14.9.31.2
      *> SEND and RECEIVE are the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4
      *> item 1; Annex A.3 item 4): declining it declines its behaviour, not the syntax rules of its statements, and
      *> the statement still reads its operands (kb/Work PB3568, PB4118).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4118A1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "HELLO".
       01 WS-REPLY PIC X(8) VALUE "NONE".
       01 WS-TAG   PIC X(8) VALUE "TAG".
       01 WS-LEN   PIC 9(4) VALUE 0.
       01 GRP-B.
          05 FLD   PIC X(8) VALUE "FLD".
       01 GRP.
          05 TBL   PIC X(8) OCCURS 3 VALUE "TBL".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO FLD OF GRP-B FROM WS-MSG
           END-SEND.
           DISPLAY "LEG1=" FLD OF GRP-B.
           SEND TO SRV FROM TBL(2) RETURNING WS-REPLY
           END-SEND.
           DISPLAY "LEG2=" WS-REPLY.
           RECEIVE FROM WS-TAG GIVING TBL(3) WS-LEN
           END-RECEIVE.
           DISPLAY "LEG3=" TBL(3) WS-LEN.
           STOP RUN.
