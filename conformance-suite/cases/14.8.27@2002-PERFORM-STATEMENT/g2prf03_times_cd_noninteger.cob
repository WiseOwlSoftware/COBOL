      *>--- conformance-case
      *> id: g2prf03_times_cd_noninteger
      *> rules: [2002-SR-14.8.27.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.27.2 SR2 - "Identifier-1 shall be an integer."
      *>   IN9V 9V9, an item of the record under an input CD
      *>   (§2002:A.1.2.2 SR5), is numeric but not an integer: IN9V
      *>   TIMES is refused at the PERFORM line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-N  PIC 9(4) VALUE 0.
       01 T.
          05 TE PIC X OCCURS 9 INDEXED BY TX.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ
           TEXT LENGTH IS TL.
       01 INREC.
          05 FILLER PIC X(12).
          05 IN4    PIC 9(4).
          05 IN9V   PIC 9V9.
          05 FILLER PIC X(69).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM IN9V TIMES
               ADD 1 TO W-N
           END-PERFORM.
           STOP RUN.
