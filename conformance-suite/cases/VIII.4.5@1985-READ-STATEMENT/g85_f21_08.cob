      *>--- conformance-case
      *> id: g85_f21_08
      *> rules: [1985-SR-VIII.4.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.3 SR2 - "Format 1 must be used for all files in
      *> sequential access mode". A refusal: FX is a relative file in
      *> sequential access mode and the READ at line 33 is format 2
      *> (INVALID KEY), which the rule forbids; WiseOwl refuses it at 85
      *> with COBOL1720 there, and a compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2108.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "G85F2108.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS WS-RK
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  FX
           LABEL RECORDS ARE STANDARD.
       01  FX-REC PIC X(3).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       01  WS-RK PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT FX.
           READ FX RECORD
               INVALID KEY DISPLAY "INVALID"
           END-READ.
           CLOSE FX.
           STOP RUN.
