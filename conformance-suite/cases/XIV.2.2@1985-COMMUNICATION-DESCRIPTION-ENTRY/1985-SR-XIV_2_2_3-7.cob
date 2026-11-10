      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-7
      *> rules: [1985-SR-XIV.2.2.3-7]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR7 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDORDER.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CD-OUT FOR OUTPUT
           SYMBOLIC DESTINATION IS CD-DEST
           STATUS KEY IS CD-STAT
           DESTINATION TABLE OCCURS 2 TIMES
           TEXT LENGTH IS CD-LEN
           ERROR KEY IS CD-ERR
           DESTINATION COUNT IS CD-COUNT.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "CD OK".
           STOP RUN.
