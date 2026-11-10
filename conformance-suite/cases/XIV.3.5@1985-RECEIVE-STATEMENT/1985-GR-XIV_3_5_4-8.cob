      *>--- conformance-case
      *> id: 1985-GR-XIV_3_5_4-8
      *> rules: [1985-GR-XIV.3.5.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.3.5.4 GR8 (THE RECEIVE STATEMENT). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the triage of the drafts that did not
      *> match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RCVMSG.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-AREA.
           05  MSG-CHAR PIC X OCCURS 80 TIMES.
       01  I PIC 999.
       01  START-POS PIC 999.
       01  RESULT PIC X(4) VALUE "PASS".
       01  GOT-MSG PIC X VALUE "N".
       COMMUNICATION SECTION.
       CD  CD-IN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SUB-QUEUE-1 IS IN-SUB1
           SYMBOLIC SUB-QUEUE-2 IS IN-SUB2
           SYMBOLIC SUB-QUEUE-3 IS IN-SUB3
           TEXT LENGTH IS IN-LENGTH
           END KEY IS IN-END-KEY
           STATUS KEY IS IN-STATUS.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE ALL "*" TO MSG-AREA.
           MOVE "QUEUE1" TO IN-QUEUE.
           MOVE SPACES TO IN-SUB1 IN-SUB2 IN-SUB3.
           MOVE "Y" TO GOT-MSG.
           RECEIVE CD-IN MESSAGE INTO MSG-AREA
               NO DATA MOVE "N" TO GOT-MSG.
           IF GOT-MSG = "Y" AND IN-STATUS = "00" AND IN-LENGTH < 80
               COMPUTE START-POS = IN-LENGTH + 1
               PERFORM CHECK-PARA VARYING I FROM START-POS BY 1
                   UNTIL I > 80.
           DISPLAY RESULT.
           STOP RUN.
       CHECK-PARA.
           IF MSG-CHAR (I) NOT = "*"
               MOVE "FAIL" TO RESULT.
