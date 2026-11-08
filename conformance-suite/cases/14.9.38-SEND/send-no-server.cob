      *>--- conformance-case
      *> id: send-no-server
      *> rules: [GR-14.9.38.4-2, GR-14.9.38.4-3, GR-14.9.38.4-5, GR-14.9.38.4-6]
      *> accept: [2023]
      *> requires: [A.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.38.4 GR5: "Message-server-name-1 or literal-1 identify a message server run unit. If such a run
      *> unit is not identified, the EC-MCS-NO-SERVER exception condition is set to exist, and the execution of the
      *> SEND statement is unsuccessful."  GR3 a): an unsuccessful SEND with an ON EXCEPTION phrase transfers control
      *> to imperative-statement-1.  GR2: a successful SEND ignores ON EXCEPTION.  GR6: data-name-1 is set only when
      *> the SEND is successful.  14.6.13.1.1: the condition is set to exist only if checking for it is enabled (here
      *> by the TURN directive), and NOTE 3 there leaves the results of an operation with checking off undefined as to
      *> the condition, so the ON EXCEPTION phrase is taken in both legs while EXCEPTION-STATUS names the condition
      *> only in the leg with checking on.
      *>   cite.py: OK  14.9.38.4 GR5  OK  14.9.38.4 GR3  OK  14.9.38.4 GR6  OK  14.6.13.1.1
      *> The test names no server and is itself a run unit that serves none; nothing can answer.
      *> EXPECTED VALUES, DERIVED:
      *>   leg 1 (checking on): the SEND is unsuccessful: ON EXCEPTION runs and EXCEPTION-STATUS is EC-MCS-NO-SERVER;
      *>         the tag stays NULL (GR6), and the NOT ON EXCEPTION phrase does not run.
      *>   leg 2 (checking off): the SEND is unsuccessful as well, so ON EXCEPTION runs and the tag stays NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDNOSRV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 REQ-REC   PIC X(8) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-MCS-NO-SERVER CHECKING ON
           SEND TO "NOSUCHSERVER" FROM REQ-REC RETURNING MT
               ON EXCEPTION DISPLAY "LEG1 EXC " FUNCTION TRIM(
                   FUNCTION EXCEPTION-STATUS)
               NOT ON EXCEPTION DISPLAY "LEG1 OK"
           END-SEND.
           IF MT = NULL DISPLAY "LEG1 TAG=NULL" END-IF.
           >>TURN EC-MCS-NO-SERVER CHECKING OFF
           SEND TO "NOSUCHSERVER" FROM REQ-REC RETURNING MT
               ON EXCEPTION DISPLAY "LEG2 EXC"
               NOT ON EXCEPTION DISPLAY "LEG2 OK"
           END-SEND.
           IF MT = NULL DISPLAY "LEG2 TAG=NULL" END-IF.
           STOP RUN.
