      *>--- conformance-case
      *> id: g2prf02_varying_cd_alphanumeric
      *> rules: [2002-SR-14.8.27.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.27.2 SR2 - "Each identifier shall reference a
      *>   numeric elementary item". SQ, the SYMBOLIC QUEUE of the
      *>   input CD, is alphanumeric X(12) (§2002:A.1.2.3 GR2 a):
      *>   VARYING SQ is refused at the PERFORM line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF02.
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
           PERFORM VARYING SQ FROM 1 BY 1 UNTIL W-N > 1
               ADD 1 TO W-N
           END-PERFORM.
           STOP RUN.
