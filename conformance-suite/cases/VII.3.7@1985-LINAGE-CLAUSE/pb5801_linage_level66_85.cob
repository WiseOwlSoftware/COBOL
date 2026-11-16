      *>--- conformance-case
      *> id: pb5801_linage_level66_85
      *> rules: [1985-SR-VII.3.7.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §1985:VII.3.7.3 SR1: "Data-name-1, data-name-2,
      *>   data-name-3, data-name-4 must reference elementary unsigned
      *>   numeric integer data items". Each is a level 66 entry renaming
      *>   one PIC 99 item, and §1985:VI.5.11.4 GR2 gives it "all of the
      *>   data attributes of data-name-2", so each operand is legal.
      *> DERIVATION. Page body 4 (L66), footing area from line 3 (F66,
      *>   §1985:VII.3.7.4 GR3). §1985:VII.3.7.4 GR9: LINAGE-COUNTER is
      *>   "set to one at the time an OPEN statement with the OUTPUT
      *>   phrase" is executed, and each WRITE BEFORE ADVANCING 1 LINE
      *>   increments it by 1: 2, 3, 4. §1985:VII.4.7.4 GR18: the
      *>   end-of-page condition occurs when a WRITE with END-OF-PAGE
      *>   makes LINAGE-COUNTER "equal or exceed" data-name-2's value:
      *>   not on the first WRITE (2), on the second (3) and third (4);
      *>   4 does not exceed the page body, so no page overflow.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801) or reads a LINAGE value from anywhere but the storage
      *>   each 66 entry renames.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801LN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO P5801LN.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD
           LINAGE IS L66 LINES WITH FOOTING AT F66
               LINES AT TOP T66 LINES AT BOTTOM B66.
       01  R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01  G1.
           02  G-L PIC 99 VALUE 4.
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
           MOVE "LINE THREE" TO R1.
           WRITE R1 BEFORE ADVANCING 1 LINE
               AT END-OF-PAGE DISPLAY "EOP-3"
               NOT AT END-OF-PAGE DISPLAY "NOEOP-3"
           END-WRITE.
           MOVE LINAGE-COUNTER TO W-LC.
           DISPLAY "LC " W-LC.
           CLOSE F1.
           STOP RUN.
