      *>--- conformance-case
      *> id: 1985-GR-IV_6_4_2_1_1-3
      *> rules: [1985-GR-IV.6.4.2.1.1-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.6.4.2.1.1 GR3 (Conditional Statements and Sentences).
      *> An output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the triage of the
      *> drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CONDWR.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE.
       01  IREC.
           05 IKEY  PIC X(4).
           05 IDATA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "0001" TO IKEY.
           MOVE "A" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "BAD1"
               NOT INVALID KEY DISPLAY "OK1" DISPLAY "OK1B".
           MOVE "0001" TO IKEY.
           MOVE "B" TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "DUP2" DISPLAY "DUP2B"
               NOT INVALID KEY DISPLAY "OK2" DISPLAY "OK2B".
           DISPLAY "END".
           CLOSE IFILE.
           STOP RUN.
