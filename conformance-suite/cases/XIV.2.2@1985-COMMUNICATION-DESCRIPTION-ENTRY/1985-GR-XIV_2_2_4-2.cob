      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-2
      *> rules: [1985-GR-XIV.2.2.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR2 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDTEST.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS CQ
           SYMBOLIC SUB-QUEUE-1 IS CS1
           SYMBOLIC SUB-QUEUE-2 IS CS2
           SYMBOLIC SUB-QUEUE-3 IS CS3
           MESSAGE DATE IS CDT
           MESSAGE TIME IS CTM
           SYMBOLIC SOURCE IS CSRC
           TEXT LENGTH IS CTL
           END KEY IS CEK
           STATUS KEY IS CSK
           MESSAGE COUNT IS CMC.
       01  CDIN-REC PIC X(87).
       CD  CDSER FOR INPUT
           SQ SS1 SS2 SS3 SDT STM SSRC STL SEK SSK SMC.
       01  CDSER-REC PIC X(87).
       PROCEDURE DIVISION.
       P1.
           MOVE "QUEUE-NAME-1" TO CQ.
           MOVE "SUBQUEUE-1AA" TO CS1.
           MOVE "SUBQUEUE-2BB" TO CS2.
           MOVE "SUBQUEUE-3CC" TO CS3.
           MOVE 991231 TO CDT.
           MOVE 23595999 TO CTM.
           MOVE "SOURCE-TERM1" TO CSRC.
           MOVE 123 TO CTL.
           MOVE "E" TO CEK.
           MOVE "ST" TO CSK.
           MOVE 456 TO CMC.
           DISPLAY CDIN-REC.
           MOVE "ABCDEFGHIJKLMN" TO CQ.
           MOVE -5 TO CMC.
           MOVE "XY" TO CEK.
           MOVE "PQR" TO CSK.
           DISPLAY CQ.
           DISPLAY CMC.
           DISPLAY CEK.
           DISPLAY CSK.
           MOVE ALL "-" TO CDSER-REC.
           MOVE "Q" TO SQ.
           MOVE "A" TO SS1.
           MOVE "B" TO SS2.
           MOVE "C" TO SS3.
           MOVE 1 TO SDT.
           MOVE 2 TO STM.
           MOVE "S" TO SSRC.
           MOVE 3 TO STL.
           MOVE "K" TO SEK.
           MOVE "OK" TO SSK.
           MOVE 4 TO SMC.
           DISPLAY CDSER-REC.
           STOP RUN.
