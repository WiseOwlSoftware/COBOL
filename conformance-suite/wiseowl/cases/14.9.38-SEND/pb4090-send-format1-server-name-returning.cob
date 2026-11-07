      *>--- conformance-case
      *> id: pb4090-send-format1-server-name-returning
      *> rules: [FMT-14.9.38.2, SR-14.9.38.3-2]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A Format 1 SEND (to-message-server) whose TO operand is a message-server-name-1, with its RETURNING phrase.
      *> ISO 14.9.38.2 Format 1 prints "SEND TO { literal-1 / message-server-name-1 } FROM identifier-1" and then
      *> "RETURNING data-name-1" without brackets, and 14.9.38.3 SR2 says "Message-server-name-1 is the name of a
      *> message server run unit" - a processor-defined name, declared in no data division.
      *>   cite.py: OK  14.9.38.3 2)  (Syntax rules)  OK  14.9.38.2  (General formats)
      *> Each leg fails if the binder resolves the TO name as a data name, which would refuse legal source (kb/Work
      *> PB4090): SRVNAME and SRVTWO are declared nowhere, and a server name is no data item, so neither is an error.
      *>   leg 1  SEND TO message-server-name-1 FROM identifier-1 RETURNING data-name-1;
      *>   leg 2  the same with the optional word TO omitted (14.9.38.2 prints TO without an underline, 5.2.3).
      *> SEND is the asynchronous messaging facility (Annex A.3 item 4). No run unit serves the names sent to, so each SEND is
      *> EC-MCS-NO-SERVER and unsuccessful (14.9.38.4 GR5). EXPECTED VALUES, DERIVED: nothing is received, so WS-REPLY stays NULL
      *> (14.9.38.4 GR6 sets the tag only when a server run unit takes the message) and the two tests of WS-REPLY find it NULL, as
      *> it began.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4090F1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "HELLO".
       01 WS-REPLY USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO SRVNAME FROM WS-MSG RETURNING WS-REPLY
           END-SEND.
           IF WS-REPLY = NULL DISPLAY "LEG1=NULL" END-IF.
           SEND SRVTWO FROM WS-MSG RETURNING WS-REPLY
           END-SEND.
           IF WS-REPLY = NULL DISPLAY "LEG2=NULL" END-IF.
           STOP RUN.
