      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-3
      *> rules: [1985-SR-XIV.2.2.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR3 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDORDR.
       ENVIRONMENT DIVISION.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           STATUS KEY IS CD-STAT
           END KEY IS CD-END
           MESSAGE COUNT IS CD-COUNT
           SYMBOLIC QUEUE IS CD-QUEUE.
       PROCEDURE DIVISION.
       P1.
           DISPLAY "CD CLAUSES ACCEPTED".
           STOP RUN.
