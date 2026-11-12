      *>--- conformance-case
      *> id: pb5290-cd-record-standard-data-format-size
      *> rules: [1985-SR-XIV.2.2.3-5, 1985-SR-XIV.2.2.3-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5290. §1985:XIV.2.2.3 SR5: the record after an input CD
      *> "must describe a record of exactly 87 standard data format
      *> characters", and SR14 33 for an input-output CD; "The size of an
      *> elementary data item or a group item is the number of characters
      *> in standard data format of the item" (§1985:IV.4.3.4), so a
      *> COMP item counts its digits, not the storage it occupies.
      *> X(79) + 9(8) COMP is 79 + 8 = 87 characters, and X(29) + 9(4)
      *> COMP is 29 + 4 = 33: both CDs are legal, and the program runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5290OK.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CI FOR INPUT
           STATUS KEY IS CI-ST.
       01  R1.
           02 R1-A PIC X(79).
           02 R1-B PIC 9(8) COMP.
       CD TCD FOR I-O
           STATUS KEY IS TSTAT.
       01  R2.
           02 R2-A PIC X(29).
           02 R2-B PIC 9(4) COMP.
       PROCEDURE DIVISION.
       P1.
           MOVE "AB" TO CI-ST.
           DISPLAY CI-ST.
           STOP RUN.
