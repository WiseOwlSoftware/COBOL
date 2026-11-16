      *>--- conformance-case
      *> id: g85_z06
      *> rules: [1985-GR-VI.6.5.4-2, 1985-GR-VI.6.5.4-3,
      *>    1985-GR-VI.6.5.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *> stdin: g85_z06.in
      *>---
      *> §1985:VI.6.5.4 GR3 - a transfer of the receiver's own size
      *> is stored in it whole: an 80-character record (GR2's size,
      *> docs/conformance/7/DOC-A.1-5.md) into PIC X(80) preset to Q,
      *> lines 1 and 2: a lost, shifted or truncated character or a
      *> surviving Q changes them (line 2 reads the same under a
      *> whole-line transfer, which GR4 b also cuts to 80).
      *> Line 4 is GR4 a (X(160) stores a record and requests more)
      *> plus the determination for a portion one record fills
      *> exactly; it is not GR3's. Lines 3 and 5: every ACCEPT begins
      *> at a new line of the device (DOC-A.1-5.md as corrected);
      *> line 6: end of input leaves spaces (DOC-A.1-5.md).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Z06.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R80   PIC X(80) VALUE ALL "Q".
       01  R5    PIC X(5).
       01  R160  PIC X(160) VALUE ALL "Q".
       PROCEDURE DIVISION.
       P1.
           ACCEPT R80.
           DISPLAY "1[" R80 "]".
           MOVE ALL "Q" TO R80.
           ACCEPT R80.
           DISPLAY "2[" R80 "]".
           ACCEPT R5.
           DISPLAY "3[" R5 "]".
           ACCEPT R160.
           DISPLAY "4[" R160 "]".
           ACCEPT R5.
           DISPLAY "5[" R5 "]".
           MOVE "QQQQQ" TO R5.
           ACCEPT R5.
           DISPLAY "6[" R5 "]".
           STOP RUN.
