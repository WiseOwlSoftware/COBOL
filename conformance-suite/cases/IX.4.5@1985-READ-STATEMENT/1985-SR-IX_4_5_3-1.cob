      *>--- conformance-case
      *> id: 1985-SR-IX_4_5_3-1
      *> rules: [1985-SR-IX.4.5.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.3 SR1 (THE READ STATEMENT). A refusal: the rule
      *> forbids line 22 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6827 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDINTO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO DISK
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IKEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IKEY  PIC X(4).
           05  IDATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WREC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IFILE.
           READ IFILE RECORD INTO IREC
               AT END DISPLAY "EOF".
           CLOSE IFILE.
           STOP RUN.
