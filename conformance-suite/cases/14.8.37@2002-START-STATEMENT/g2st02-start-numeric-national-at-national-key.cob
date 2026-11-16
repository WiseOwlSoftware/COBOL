      *>--- conformance-case
      *> id: g2st02-start-numeric-national-at-national-key
      *> rules: [2002-SR-14.8.37.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.37.2 SR6 - an indexed START's data-name-1 that is not
      *>   itself a record key shall have "the same class, category, and
      *>   usage as that record key". NUM2 PIC 9(2) USAGE NATIONAL begins
      *>   at the leftmost position of the alternate key NK PIC N(6) and
      *>   shares its usage, but it is category and class numeric
      *>   (§2002:8.5.2.9 item 1; kb/Work PB5625 reading B) where NK is
      *>   national, so the START at line 38 is refused. This refusal
      *>   stands beside the accepted case g2st01-indexed-key-operand-
      *>   length; it is not the closing witness.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ST02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2st02.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS NK.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  REC.
           05  PK              PIC X(6).
           05  NK              PIC N(6).
           05  NK-R REDEFINES NK.
               10  NUM2        PIC 9(2) USAGE NATIONAL.
               10  FILLER      PIC N(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT F1
           START F1 KEY = NUM2
           CLOSE F1
           STOP RUN.
