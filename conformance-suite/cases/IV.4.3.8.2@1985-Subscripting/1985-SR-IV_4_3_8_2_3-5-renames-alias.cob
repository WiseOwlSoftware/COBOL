      *>--- conformance-case
      *> id: 1985-SR-IV_4_3_8_2_3-5-renames-alias
      *> rules: [1985-SR-IV.4.3.8.2.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5477 - A LEVEL-66 RENAMES OF ONE NUMERIC INTEGER ITEM AS A SUBSCRIPT.
      *> §1985:IV.4.3.8.2.3 SR5: "Data-name-2 may be qualified and must be a numeric
      *> elementary item representing an integer." §1985:VI.5.11.4 GR2: "all of the
      *> data attributes of data-name-2 become the data attributes for data-name-1",
      *> so RN (RENAMES GA, GA PIC 9) is a numeric elementary integer item and a legal
      *> subscript and reference-modification position (§1985:IV.4.3.8.4).
      *>   GA = 3: ELT (RN) is ELT (3) = "C"; ADD 1 TO RN makes GA 4, so ELT (RN) is
      *>   "D", ELT (RN - 1) is "C", and TBL (RN:2) is positions 4-5 = "DE".
      *> Each DISPLAY fails if the alias is read as anything but GA: the generated
      *> program did not compile before this fix.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SUBRNAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  TBL.
           05  ELT PIC X OCCURS 5 TIMES.
       01  GR.
           05 GA PIC 9 VALUE 3.
           05 GB PIC 9 VALUE 4.
       66  RN RENAMES GA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "ABCDE" TO TBL.
           DISPLAY "J1 " ELT (RN).
           ADD 1 TO RN.
           DISPLAY "J2 " ELT (RN).
           DISPLAY "J3 " ELT (RN - 1).
           DISPLAY "J4 " TBL (RN:2).
           STOP RUN.
