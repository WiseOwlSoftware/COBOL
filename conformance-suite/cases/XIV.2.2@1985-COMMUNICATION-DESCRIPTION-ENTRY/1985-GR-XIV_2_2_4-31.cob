      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-31
      *> rules: [1985-GR-XIV.2.2.4-31]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR31 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. GR31IO.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  MSG-TEXT PIC X(5) VALUE "HELLO".
       COMMUNICATION SECTION.
       CD  IOCD FOR I-O
           MESSAGE DATE IS IO-DATE
           MESSAGE TIME IS IO-TIME
           SYMBOLIC TERMINAL IS IO-TERM
           TEXT LENGTH IS IO-LEN
           END KEY IS IO-END
           STATUS KEY IS IO-STAT.
       PROCEDURE DIVISION.
       P1.
           MOVE "TERM01" TO IO-TERM.
           MOVE 5 TO IO-LEN.
           SEND IOCD FROM MSG-TEXT WITH EMI.
           MOVE "TERM02" TO IO-TERM.
           MOVE "XX" TO IO-STAT.
           SEND IOCD FROM MSG-TEXT WITH EMI.
           IF IO-STAT NOT = "00"
               DISPLAY "UNSUCCESSFUL"
           ELSE
               DISPLAY "SUCCESSFUL".
           DISPLAY IO-STAT.
           STOP RUN.
