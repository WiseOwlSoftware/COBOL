      *>--- conformance-case
      *> id: g2prf10_from_index_by_literal_noninteger
      *> rules: [2002-SR-14.8.27.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.27.2 SR6 c - with an index-name in FROM, "The
      *>   literal in the associated BY phrase shall be an integer."
      *>   BY 1.5 is not an integer: refused at the PERFORM line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF10.
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
           PERFORM VARYING IN4 FROM TX BY 1.5 UNTIL IN4 > 2
               ADD 1 TO W-N
           END-PERFORM.
           STOP RUN.
