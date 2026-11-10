      *>--- conformance-case
      *> id: 1985-GR-XV_3_2_4-8
      *> rules: [1985-GR-XV.3.2.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XV.3.2.4 GR8 (THE USE FOR DEBUGGING STATEMENT). An output
      *> test: the program must translate and run at 85 and DISPLAY
      *> exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DBGCD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. ANYCOMP WITH DEBUGGING MODE.
       OBJECT-COMPUTER. ANYCOMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-MSG PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD CD-IN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           MESSAGE COUNT IS IN-COUNT.
       CD CD-OUT FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LENGTH
           SYMBOLIC DESTINATION IS OUT-DEST.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-CD SECTION.
           USE FOR DEBUGGING ON CD-IN.
       DBG-PARA.
           DISPLAY "DEBUG CD-IN".
       DBG-CDO SECTION.
           USE FOR DEBUGGING ON CD-OUT.
       DBG-PARA-O.
           DISPLAY "DEBUG CD-OUT".
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       MAIN-PARA.
           MOVE "QUEUE1" TO IN-QUEUE.
           ENABLE INPUT CD-IN.
           DISPLAY "ENABLED".
           ACCEPT CD-IN MESSAGE COUNT.
           DISPLAY "COUNTED".
           DISABLE INPUT CD-IN.
           DISPLAY "DISABLED".
           MOVE 1 TO OUT-COUNT.
           MOVE 5 TO OUT-LENGTH.
           MOVE "DEST1" TO OUT-DEST.
           SEND CD-OUT FROM W-MSG WITH EMI.
           DISPLAY "SENT".
           STOP RUN.
