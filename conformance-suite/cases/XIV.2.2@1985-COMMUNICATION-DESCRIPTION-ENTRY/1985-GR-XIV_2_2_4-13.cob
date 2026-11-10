      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-13
      *> rules: [1985-GR-XIV.2.2.4-13]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR13 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDSTAT.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS CD-QUEUE
           STATUS KEY IS CD-STATUS
           MESSAGE COUNT IS CD-COUNT.
       PROCEDURE DIVISION.
       P1.
           MOVE "NOSUCHQUEUE" TO CD-QUEUE.
           ACCEPT CDIN MESSAGE COUNT.
           DISPLAY CD-STATUS.
           STOP RUN.
