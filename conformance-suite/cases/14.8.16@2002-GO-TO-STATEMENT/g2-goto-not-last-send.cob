      *>--- conformance-case
      *> id: g2-goto-not-last-send
      *> rules: [2002-SR-14.8.16.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.16.2 SR2 - a format 1 GO TO "shall appear as the last
      *>   statement in that sequence". SEND (2002-only, communication
      *>   facility, table 13: no conditional phrase) is imperative
      *>   (§2002:14.4), so GO TO P1 SEND CDOUT FROM X puts the GO TO
      *>   before another imperative statement in its sequence: refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GOTO02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X   PIC X(4) VALUE "MSG1".
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO P1 SEND CDOUT FROM X.
       P1.
           DISPLAY "P1" STOP RUN.
