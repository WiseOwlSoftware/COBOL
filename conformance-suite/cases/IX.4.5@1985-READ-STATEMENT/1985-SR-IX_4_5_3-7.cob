      *>--- conformance-case
      *> id: 1985-SR-IX_4_5_3-7
      *> rules: [1985-SR-IX.4.5.3-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.3 SR7 (THE READ STATEMENT). A refusal: the rule
      *> forbids line 19 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RDTEST.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO IXDATA
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IREC-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IREC-KEY  PIC X(5).
           05  IREC-DATA PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IFILE.
           READ IFILE.
           CLOSE IFILE.
           STOP RUN.
