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
      *> SEND is the declined asynchronous messaging facility (COBOL1578, docs/CONFORMANCE.md section 4 item 1;
      *> Annex A.3 item 4): each SEND is accepted and performs no message I-O, so the program's output is its own
      *> DISPLAYs. EXPECTED VALUES, DERIVED: nothing is sent or received, so WS-REPLY keeps its VALUE "NONE" and the
      *> two DISPLAYs print what the program started with.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4090F1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-MSG   PIC X(8) VALUE "HELLO".
       01 WS-REPLY PIC X(8) VALUE "NONE".
       PROCEDURE DIVISION.
       MAIN-P.
           SEND TO SRVNAME FROM WS-MSG RETURNING WS-REPLY
           END-SEND.
           DISPLAY "LEG1=" WS-REPLY.
           SEND SRVTWO FROM WS-MSG RETURNING WS-REPLY
           END-SEND.
           DISPLAY "LEG2=" WS-REPLY.
           STOP RUN.
