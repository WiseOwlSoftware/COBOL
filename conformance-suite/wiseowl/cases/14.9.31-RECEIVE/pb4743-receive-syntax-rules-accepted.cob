      *>--- conformance-case
      *> id: pb4743-receive-syntax-rules-accepted
      *> rules: [FMT-14.9.31.2, SR-14.9.31.3-1, SR-14.9.31.3-2, SR-14.9.31.3-3]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The legal side of RECEIVE's syntax rules (ISO 14.9.31.3), each leg a statement the screens must NOT refuse:
      *>   SR1  data-name-1 is a message-tag data item: MT, USAGE MESSAGE-TAG.
      *>   SR2  identifier-1 and its subordinates carry none of the forbidden clauses, and an OCCURS DEPENDING ON whose
      *>        object lies WITHIN identifier-1 is none of them: REC-IN holds CNT and the table that depends on it.
      *>   SR3  data-name-2 is an integer data item of the class numeric: WS-LEN, PIC 9(4).
      *>   14.9.31.2  CONTINUE AFTER arithmetic-expression-1 SECONDS and CONTINUE AFTER MESSAGE RECEIVED.
      *>   cite.py: OK  14.9.31.3 1)  OK  14.9.31.3 2)  OK  14.9.31.3 3)  OK  14.9.31.2  "MESSAGE RECEIVED"
      *> RECEIVE is the asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1; Annex A.3
      *> item 4): the statement is accepted and performs no message I-O until the exchange is built (kb/Work PB4744),
      *> so the program's output is its own DISPLAYs. EXPECTED VALUES, DERIVED: nothing is received, so WS-LEN keeps
      *> its VALUE 7, REC-FLAT keeps "FLAT" and CNT keeps 3; the tag stays NULL (13.18.63.4 GR4 gives a message-tag
      *> item the null content), and 8.8.4.2.14 makes the relation to NULL true.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 REC-IN.
          05 CNT    PIC 9 VALUE 3.
          05 ITEMS  PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 REC-FLAT  PIC X(8) VALUE "FLAT".
       01 WS-LEN    PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           RECEIVE MT GIVING REC-IN WS-LEN
           END-RECEIVE.
           RECEIVE FROM MT GIVING REC-FLAT WS-LEN
               CONTINUE AFTER 2 SECONDS
               ON EXCEPTION DISPLAY "EXC"
               NOT ON EXCEPTION DISPLAY "OK"
           END-RECEIVE.
           RECEIVE MT GIVING REC-FLAT WS-LEN
               CONTINUE AFTER MESSAGE RECEIVED
           END-RECEIVE.
           IF MT = NULL DISPLAY "TAG=NULL" END-IF.
           DISPLAY "LEN=" WS-LEN.
           DISPLAY "FLAT=" REC-FLAT.
           DISPLAY "CNT=" CNT.
           STOP RUN.
