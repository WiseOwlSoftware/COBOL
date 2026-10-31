      *>--- conformance-case
      *> id: pb4206-go-to-reserved-verb-as-procedure-name
      *> rules: [SR-8.3.2.1-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4206 - the GO TO sibling of pb4206-perform-reserved-verb-
      *> as-procedure-name. 8.3.2.1 1): "Reserved words shall not be used
      *> as user-defined words or system-names", and 8.9 reserves SEND at
      *> COBOL-85, COBOL-2002 and COBOL-2023. Expected: COBOL0901 naming
      *> SEND. It fails when GO TO's procedure-name slot cannot hold the
      *> word and the parse reads GO TO without its operand (the 1985
      *> target-less form) followed by a SEND statement whose operand is
      *> missing: the error then falls at the period and never names SEND.
      *> No paragraph is named SEND, so COBOL1639 (8.4.2.1, the reference
      *> identifies no resource) is reported beside it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4206GOTO.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "MAIN".
           GO TO SEND.
       P-END.
           STOP RUN.
