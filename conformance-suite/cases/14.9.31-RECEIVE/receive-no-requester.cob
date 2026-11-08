      *>--- conformance-case
      *> id: receive-no-requester
      *> rules: [GR-14.9.31.4-1, GR-14.9.31.4-4, GR-14.9.31.4-5]
      *> accept: [2023]
      *> requires: [A.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.31.4 GR1 a): "If the content of data-name-1 is the value NULL before the execution of the RECEIVE
      *> statement, the RECEIVE statement is waiting for any requestor run unit to send a message to the current run
      *> unit."  A run unit that serves no message server name is addressed by no requestor, so such a RECEIVE could
      *> wait for ever; this implementation completes it at once as unsuccessful with Table 13's EC-MCS-NO-REQUESTER
      *> (docs/CONFORMANCE.md D-MCS-NR).  GR5 a): an unsuccessful RECEIVE with an ON EXCEPTION phrase transfers control
      *> to imperative-statement-1; GR4: only a successful RECEIVE runs NOT ON EXCEPTION.
      *>   cite.py: OK  14.9.31.4 GR1  OK  14.9.31.4 GR5  OK  14.6.13.1.6
      *> EXPECTED VALUES, DERIVED: the tag is NULL (13.18.63.4 GR4); the RECEIVE is unsuccessful and stores nothing, so the
      *> receiving area keeps its VALUE and the length item keeps 7; ON EXCEPTION runs and EXCEPTION-STATUS names
      *> EC-MCS-NO-REQUESTER (checking on); the tag is still NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVNOREQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 BUF       PIC X(8) VALUE "UNCHANGD".
       01 LEN       PIC 9(4) VALUE 7.
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-MCS-NO-REQUESTER CHECKING ON
           RECEIVE RT GIVING BUF LEN
               ON EXCEPTION DISPLAY "EXC " FUNCTION TRIM(
                   FUNCTION EXCEPTION-STATUS)
               NOT ON EXCEPTION DISPLAY "OK"
           END-RECEIVE.
           DISPLAY "BUF=" BUF " LEN=" LEN.
           IF RT = NULL DISPLAY "TAG=NULL" END-IF.
           STOP RUN.
