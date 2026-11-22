      *>--- conformance-case
      *> id: pb5801_linage_level66
      *> rules: [SR-13.18.34.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §2023:13.18.34.3 SR2: each data-name shall
      *>   reference an "elementary unsigned numeric integer" item. Each is
      *>   a level 66 entry renaming one PIC 99 item, and §2023:13.18.45.4
      *>   GR1 gives it "all of the data attributes of data-name-2", so
      *>   each operand is legal.
      *> DERIVATION. Page body 5 (L66), footing area from line 3 (F66).
      *>   §2023:13.18.34.4 GR7d: LINAGE-COUNTER "is automatically set
      *>   to one at the time an OPEN statement with the OUTPUT phrase is
      *>   executed"; GR7 c) 2.: each WRITE BEFORE ADVANCING 1 LINE adds
      *>   1: 2, then 3. §2023:14.9.51.4 GR26b: the end-of-page condition
      *>   occurs when "the associated LINAGE-COUNTER is equal to or
      *>   exceeds the current value of the footing start": not on the
      *>   first WRITE (2), on the second (3).
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801) or reads a LINAGE value from anywhere but the storage
      *>   each 66 entry renames.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801L2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO P5801L2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LINAGE IS L66 LINES WITH FOOTING AT F66
               LINES AT TOP T66 LINES AT BOTTOM B66.
       01  R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01  G1.
           02  G-L PIC 99 VALUE 5.
           02  G-F PIC 99 VALUE 3.
           02  G-T PIC 99 VALUE 1.
           02  G-B PIC 99 VALUE 1.
       66  L66 RENAMES G-L.
       66  F66 RENAMES G-F.
       66  T66 RENAMES G-T.
       66  B66 RENAMES G-B.
       01  W-LC PIC 9.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE LINAGE-COUNTER TO W-LC.
           DISPLAY "OPEN " W-LC.
           MOVE "LINE ONE" TO R1.
           WRITE R1 BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP-1"
               NOT AT END-OF-PAGE DISPLAY "NOEOP-1"
           END-WRITE.
           MOVE LINAGE-COUNTER TO W-LC.
           DISPLAY "LC " W-LC.
           MOVE "LINE TWO" TO R1.
           WRITE R1 BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP-2"
               NOT AT END-OF-PAGE DISPLAY "NOEOP-2"
           END-WRITE.
           MOVE LINAGE-COUNTER TO W-LC.
           DISPLAY "LC " W-LC.
           CLOSE F1.
           STOP RUN.
