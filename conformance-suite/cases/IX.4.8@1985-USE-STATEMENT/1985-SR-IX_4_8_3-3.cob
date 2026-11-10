      *>--- conformance-case
      *> id: 1985-SR-IX_4_8_3-3
      *> rules: [1985-SR-IX.4.8.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.8.3 SR3 (THE USE STATEMENT). A refusal: the rule
      *> forbids line 30 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL0897 there; a compiler that accepts it fails.
      *> Placed from written and run by the triage of the drafts that
      *> did not match (a suspect draft corrected) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USEDUP.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IDX-FILE ASSIGN TO "IDXFILE"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IDX-KEY.
           SELECT IDX-FILE2 ASSIGN TO "IDXFIL2"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IDX2-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IDX-FILE.
       01  IDX-REC.
           05 IDX-KEY PIC X(4).
           05 IDX-DATA PIC X(10).
       FD  IDX-FILE2.
       01  IDX2-REC.
           05 IDX2-KEY PIC X(4).
           05 IDX2-DATA PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-ONE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IDX-FILE.
       ERR-ONE-PARA.
           DISPLAY "ERR ONE".
       ERR-TWO SECTION. USE AFTER STANDARD ERROR PROCEDURE ON IDX-FILE.
       ERR-TWO-PARA.
           DISPLAY "ERR TWO".
       END DECLARATIVES.
       MAIN-LINE SECTION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
