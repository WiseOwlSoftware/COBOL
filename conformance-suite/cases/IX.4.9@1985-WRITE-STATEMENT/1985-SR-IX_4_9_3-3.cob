      *>--- conformance-case
      *> id: 1985-SR-IX_4_9_3-3
      *> rules: [1985-SR-IX.4.9.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.3 SR3 (THE WRITE STATEMENT). A refusal: the rule
      *> forbids line 22 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL6826 there; a compiler that accepts it fails.
      *> Placed from written and run by the 1985 audit's drafts pass (a
      *> draft that mismatched on an older build, re-run to MATCH with
      *> its expectation unchanged, witnesses-85g) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WRTIX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO IXF
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE
           LABEL RECORDS ARE STANDARD.
       01  IX-REC.
           05 IX-KEY PIC X(4).
           05 IX-DATA PIC X(10).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IX-FILE.
           MOVE "0001" TO IX-KEY.
           MOVE "DATA" TO IX-DATA.
           WRITE IX-REC.
           CLOSE IX-FILE.
           STOP RUN.
