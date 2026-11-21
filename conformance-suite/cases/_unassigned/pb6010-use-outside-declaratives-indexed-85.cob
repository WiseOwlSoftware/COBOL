      *>--- conformance-case
      *> id: pb6010-use-outside-declaratives-indexed-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6010 - a USE statement naming an indexed file, written outside the
      *> declaratives, breaks the indexed I-O module's own USE rule, §1985:IX.4.8.3 SR1 "A USE
      *> statement, when present, must immediately follow a section header in the declaratives
      *> portion of the Procedure Division" (it cited the sequential module's VII.4.6.3 SR1).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6010US.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "PB6010IX"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS K.
       DATA DIVISION.
       FILE SECTION.
       FD  IX.
       01  IX-REC.
           05  K PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON IX.
           STOP RUN.
