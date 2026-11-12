      *>--- conformance-case
      *> id: 1985-SR-IX_4_9_3-1
      *> rules: [1985-SR-IX.4.9.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.3 SR1 (THE WRITE STATEMENT). A refusal: the rule
      *> forbids line 22 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6827 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTIDX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO DISK1
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IREC-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05 IREC-KEY  PIC X(4).
           05 IREC-DATA PIC X(6).
       WORKING-STORAGE SECTION.
       01  WREC PIC X(10) VALUE "0001ABCDEF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           WRITE IREC FROM IREC
               INVALID KEY DISPLAY "BAD KEY".
           CLOSE IFILE.
           STOP RUN.
