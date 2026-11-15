      *>--- conformance-case
      *> id: g2prf12_after_from_index_by_cd_noninteger
      *> rules: [2002-SR-14.8.27.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.27.2 SR6 b - with an index-name in FROM, "The
      *>   identifier in the associated BY phrase shall reference an
      *>   integer data item." AFTER IN4 FROM TX BY IN9V (9V9, a CD
      *>   record item) is refused at the PERFORM line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF12.
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
           PERFORM VARYING W-N FROM 1 BY 1 UNTIL W-N > 1
                   AFTER IN4 FROM TX BY IN9V UNTIL IN4 > 2
               CONTINUE
           END-PERFORM.
           STOP RUN.
