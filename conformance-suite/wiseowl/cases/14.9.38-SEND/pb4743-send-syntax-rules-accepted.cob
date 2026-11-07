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
      *>        Also legal: a constant-name standing for such a literal (13.10.3 SR2 and 13.10.4 GR1: as if the literal were written, leg 5), a
      *>        figurative constant (8.3.3.6.3 SR1, leg 6) and a function-identifier as identifier-1 (8.4.3.1.2 Format 1, leg 7).
      *>   leg 3  Format 2 with RAISING EXCEPTION exception-name-1, leg 4 with RAISING LAST.
      *>   cite.py: OK  14.9.38.3 1)  OK  14.9.38.3 3)  OK  14.9.38.3 4)  OK  14.9.38.2
      *> SEND is the asynchronous messaging facility (Annex A.3 item 4). No run unit serves the names sent to (leg 1, 2, 5, 6, 7:
      *> EC-MCS-NO-SERVER, 14.9.38.4 GR5) and MT2 is NULL (legs 3, 4: EC-MCS-NO-REQUESTER, GR7), so every SEND is unsuccessful.
      *> EXPECTED VALUES, DERIVED: both tags stay NULL (13.18.63.4 GR4; 14.9.38.4 GR6 sets MT only when a server run unit takes
      *> the message).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4743S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 MT2       USAGE MESSAGE-TAG.
       01 K-SRV     CONSTANT AS "SRV3".
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
           SEND TO K-SRV FROM REC-IN RETURNING MT
           END-SEND.
           SEND TO SPACE FROM REC-IN RETURNING MT
           END-SEND.
           SEND TO "SRV4" FROM FUNCTION UPPER-CASE("x") RETURNING MT
           END-SEND.
           IF MT = NULL DISPLAY "MT=NULL" END-IF.
           IF MT2 = NULL DISPLAY "MT2=NULL" END-IF.
           STOP RUN.
