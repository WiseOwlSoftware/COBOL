      *>--- conformance-case
      *> id: pb5290-cd-record-comp-size-at-85
      *> rules: [1985-SR-XIV.2.2.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5290. X(83) + 9(8) COMP is 83 + 8 = 91 standard data
      *> format characters (§1985:IV.4.3.4), not the 87 §1985:XIV.2.2.3
      *> SR5 requires of the record after an input CD, whatever storage
      *> the COMP item takes. Refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5290NEG.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CI FOR INPUT
           STATUS KEY IS CI-ST.
       01  R1.
           02 R1-A PIC X(83).
           02 R1-B PIC 9(8) COMP.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
