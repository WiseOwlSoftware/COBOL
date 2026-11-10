      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-10
      *> rules: [1985-GR-XIV.2.2.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR10 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR10SRC.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-AREA PIC X(80).
       COMMUNICATION SECTION.
       CD  INCD FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           SYMBOLIC SUB-QUEUE-1 IS IN-SUB-1
           SYMBOLIC SUB-QUEUE-2 IS IN-SUB-2
           SYMBOLIC SUB-QUEUE-3 IS IN-SUB-3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SOURCE
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-ENDKEY
           STATUS KEY IS IN-STATUS
           MESSAGE COUNT IS IN-COUNT.
       PROCEDURE DIVISION.
       P1.
           MOVE "INQ" TO IN-QUEUE.
           MOVE SPACES TO IN-SUB-1 IN-SUB-2 IN-SUB-3 IN-SOURCE.
           RECEIVE INCD MESSAGE INTO MSG-AREA
               NO DATA GO TO P-NODATA.
           IF IN-SOURCE = SPACES OR IN-SOURCE (1:1) NOT = SPACE
               DISPLAY "CHECKED"
           ELSE
               DISPLAY "BAD".
           STOP RUN.
       P-NODATA.
           DISPLAY "CHECKED".
           STOP RUN.
