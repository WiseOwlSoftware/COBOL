      *>--- conformance-case
      *> id: pb4743-send-syntax-rules-accepted
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-1, SR-14.9.38.3-3, SR-14.9.38.3-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The legal side of SEND's syntax rules (ISO 14.9.38.3), each leg a statement the screens must NOT refuse:
      *>   SR1  literal-1 is an alphanumeric literal (leg 1) or a national literal (leg 2).
      *>   SR3  identifier-1 and its subordinates carry none of the forbidden clauses: REC-IN (an OCCURS DEPENDING ON
      *>        whose object CNT lies within it) and REC-CONST, a CONSTANT RECORD, which SEND's list does not name
      *>        (RECEIVE's does, because it writes identifier-1; SEND only reads it).
      *>   SR4  data-name-1 and data-name-2 are message-tag data items: MT and MT2, USAGE MESSAGE-TAG.
      *>   leg 3  Format 2 with RAISING EXCEPTION exception-name-1, leg 4 with RAISING LAST.
      *>   cite.py: OK  14.9.38.3 1)  OK  14.9.38.3 3)  OK  14.9.38.3 4)  OK  14.9.38.2
      *> SEND is the asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1; Annex A.3 item
      *> 4): the statement is accepted and performs no message I-O until the exchange is built (kb/Work PB4744), so the
      *> program's output is its own DISPLAYs. EXPECTED VALUES, DERIVED: nothing is sent, so both tags stay NULL
      *> (13.18.63.4 GR4; 14.9.38.4 GR6 sets MT only when a server run unit takes the message).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 MT2       USAGE MESSAGE-TAG.
       01 REC-IN.
          05 CNT    PIC 9 VALUE 3.
          05 ITEMS  PIC X OCCURS 1 TO 5 DEPENDING ON CNT.
       01 REC-CONST CONSTANT RECORD.
          05 CC     PIC X(8) VALUE "ABC".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO "SRV1" FROM REC-IN RETURNING MT
           END-SEND.
           SEND TO N"SRV2" FROM REC-CONST RETURNING MT
           END-SEND.
           SEND TO MT2 FROM REC-IN RAISING EXCEPTION EC-MCS-INVALID-TAG
           END-SEND.
           SEND TO MT2 FROM REC-CONST RAISING LAST
           END-SEND.
           IF MT = NULL DISPLAY "MT=NULL" END-IF.
           IF MT2 = NULL DISPLAY "MT2=NULL" END-IF.
           STOP RUN.
