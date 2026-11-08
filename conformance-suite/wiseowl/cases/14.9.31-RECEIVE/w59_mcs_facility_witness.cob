*>--- conformance-case
*> id: w59_mcs_facility_witness
*> rules: [GR-14.9.31.4-4, GR-14.9.31.4-5, GR-14.9.38.4-2, GR-14.9.38.4-3]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work R43, PB1198, PB4744 - the asynchronous messaging (MCS)
      *> facility, one run unit, no server anywhere. Annex A.3 item 4
      *> makes the facility processor-dependent; WiseOwl COBOL provides
      *> it (docs/CONFORMANCE.md section 2 row 4). EXPECTED VALUES,
      *> DERIVED:
      *>   RECEIVE data-name-1 is NULL (13.18.63.4 GR4) in a run unit
      *>   that serves no server name, so no requestor can address it:
      *>   EC-MCS-NO-REQUESTER (D-MCS-NR), the RECEIVE is unsuccessful
      *>   and 14.9.31.4 GR5 takes the ON EXCEPTION imperative; the
      *>   receiving area keeps its VALUE (an unsuccessful RECEIVE
      *>   stores nothing).
      *>   SEND to a name no run unit serves: 14.9.38.4 GR5 "If such a
      *>   run unit is not identified, the EC-MCS-NO-SERVER exception
      *>   condition is set to exist", the SEND is unsuccessful and
      *>   GR3 takes the ON EXCEPTION imperative.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. W59MCS01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-TAG USAGE MESSAGE-TAG.
       01 WS-REPLY USAGE MESSAGE-TAG.
       01 WS-LEN PIC 9(4) VALUE 7.
       01 WS-BUF PIC X(10) VALUE "UNCHANGED".
       01 WS-MSG PIC X(10) VALUE "HELLO".
       PROCEDURE DIVISION.
           RECEIVE FROM WS-TAG GIVING WS-BUF WS-LEN
               ON EXCEPTION DISPLAY "RECEIVE EXC"
               NOT ON EXCEPTION DISPLAY "RECEIVE OK"
           END-RECEIVE.
           DISPLAY "AFTER RECEIVE BUF=" WS-BUF " LEN=" WS-LEN.
           SEND TO "W59SERVER" FROM WS-MSG RETURNING WS-REPLY
               ON EXCEPTION DISPLAY "SEND EXC"
               NOT ON EXCEPTION DISPLAY "SEND OK"
           END-SEND.
           DISPLAY "AFTER SEND".
           STOP RUN.
