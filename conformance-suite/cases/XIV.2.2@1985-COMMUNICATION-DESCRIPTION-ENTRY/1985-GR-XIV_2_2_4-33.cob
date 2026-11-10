      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-33
      *> rules: [1985-GR-XIV.2.2.4-33]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR33 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR33.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-AREA PIC X(2000).
       01  WS-FLAG PIC X VALUE "Y".
       COMMUNICATION SECTION.
       CD  CI FOR I-O
           MESSAGE DATE IS CI-DATE
           MESSAGE TIME IS CI-TIME
           SYMBOLIC TERMINAL IS CI-TERM
           TEXT LENGTH IS CI-LEN
           END KEY IS CI-END
           STATUS KEY IS CI-STATUS.
       PROCEDURE DIVISION.
       P1.
           MOVE "TERM01" TO CI-TERM.
           MOVE "9" TO CI-END.
           RECEIVE CI MESSAGE INTO WS-AREA
               NO DATA MOVE "N" TO WS-FLAG.
           IF WS-FLAG = "N" OR CI-STATUS NOT = "00"
               DISPLAY "VALID"
           ELSE
               IF CI-END = "0" OR "2" OR "3"
                   DISPLAY "VALID"
               ELSE
                   DISPLAY "INVALID".
           STOP RUN.
