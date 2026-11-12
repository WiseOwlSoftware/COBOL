      *>--- conformance-case
      *> id: pb5290-cd-later-record-condition-value
      *> rules: [1985-SR-XIV.2.2.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5290. §1985:XIV.2.2.3 SR9: after an output CD "only the
      *> first redefinition may contain VALUE clauses"; a condition-name
      *> entry is a VALUE clause (and SR5 and SR14 say the same of the
      *> input and input-output CD), so an 88 in the second record is
      *> refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5290NEG2.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CO FOR OUTPUT
           STATUS KEY IS CO-ST.
       01  R1.
           02 R1-A PIC X(10).
       01  R2.
           02 R2-A PIC X(10).
           02 R2-ST PIC XX.
              88 R2-OK VALUE "00".
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
