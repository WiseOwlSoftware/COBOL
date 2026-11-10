      *>--- conformance-case
      *> id: 1985-SR-IX_2_5_3-4
      *> rules: [1985-SR-IX.2.5.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.5.3 SR4 (THE ALTERNATE RECORD KEY CLAUSE). A
      *> refusal: the rule forbids line 10 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL0863 there; a compiler that
      *> accepts it fails. Placed from written and run by the triage of
      *> the drafts that did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALTKEY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO DISK1
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS REC-KEY
               ALTERNATE RECORD KEY IS ALT-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  REC-KEY PIC X(5).
           05  ALT-KEY REDEFINES REC-KEY PIC X(3).
           05  REC-DATA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
