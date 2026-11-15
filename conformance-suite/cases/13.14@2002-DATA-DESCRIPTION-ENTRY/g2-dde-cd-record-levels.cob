      *>--- conformance-case
      *> id: g2-dde-cd-record-levels
      *> rules: [2002-SR-13.14.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.14.2 SR1 - "Level-number may be 77 or 1 through 49."
      *> The permitted levels are written and run: 77 in working
      *> storage, 1 and 49 there, and 1, 5 and 49 in a record
      *> description entry after a CD (§2002:A.1.1: the CD entry is
      *> followed by record description entries, which are data
      *> description entries). A refused 77, 49 or CD record fails the
      *> compile; a wrong hierarchy for 49 shows in the CDREC line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DDE01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  W77 PIC X.
       01  W01.
           49 W49 PIC X.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 G1.
              49 A49 PIC X.
           05 A5 PIC X.
       PROCEDURE DIVISION.
           MOVE "W" TO W77
           MOVE "V" TO W49
           MOVE "Q" TO A49
           MOVE "P" TO A5
           DISPLAY "CDREC=[" CDREC "] G1=[" G1 "]"
           DISPLAY "W77=[" W77 "] W01=[" W01 "]"
           STOP RUN.
