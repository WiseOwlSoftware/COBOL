      *>--- conformance-case
      *> id: send-response-no-requester
      *> rules: [GR-14.9.38.4-3, GR-14.9.38.4-7]
      *> accept: [2023]
      *> requires: [A.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.38.4 GR7: "Data-name-2 shall contain a message tag that identifies a requestor run unit that was
      *> received by the current run unit via a RECEIVE statement and has not yet been responded to by a SEND
      *> statement in the current run unit. If data-name-2 does not identify such a run unit, ... the execution of the
      *> SEND statement is unsuccessful."  The standard prints the condition as EC-MCS-NO-RECEIVER, a name Table 13 does
      *> not list; this implementation raises Table 13's EC-MCS-NO-REQUESTER for a missing requestor
      *> (docs/CONFORMANCE.md D-MCS-NR, extended).  GR3 a): the ON EXCEPTION phrase is taken.
      *>   cite.py: OK  14.9.38.4 GR7  OK  14.9.38.4 GR3  OK  14.6.13.1.6
      *> The tag holds NULL (13.18.63.4 GR4): it identifies no requestor run unit that this run unit received.
      *> EXPECTED VALUES, DERIVED: the SEND Format 2 is unsuccessful, its ON EXCEPTION phrase runs, and with checking on
      *> for the condition EXCEPTION-STATUS names EC-MCS-NO-REQUESTER; the NOT ON EXCEPTION phrase does not run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDNOREQ.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RT        USAGE MESSAGE-TAG.
       01 REPLY-REC PIC X(8) VALUE "ANSWER".
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-MCS-NO-REQUESTER CHECKING ON
           SEND TO RT FROM REPLY-REC
               ON EXCEPTION DISPLAY "EXC " FUNCTION TRIM(
                   FUNCTION EXCEPTION-STATUS)
               NOT ON EXCEPTION DISPLAY "OK"
           END-SEND.
           STOP RUN.
