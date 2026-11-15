      *>--- conformance-case
      *> id: g2prf05_index_by_cd_noninteger
      *> rules: [2002-SR-14.8.27.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.27.2 SR5 a - with an index-name in VARYING, the
      *>   BY identifier shall reference an integer data item. BY IN9V
      *>   (9V9, a CD record item) is refused at the PERFORM line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF05.
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
           PERFORM VARYING TX FROM 1 BY IN9V UNTIL TX > 2
               ADD 1 TO W-N
           END-PERFORM.
           STOP RUN.
