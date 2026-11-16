      *>--- conformance-case
      *> id: g2-start-generic-over-source-key
      *> rules: [2002-SR-14.8.37.2-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.37.2 SR6 - for indexed files data-name-1 "shall
      *> reference either" a) a prime or alternate record key, or b) an
      *> item whose leftmost character position "corresponds to the
      *> leftmost character position of a prime or alternate record key
      *> that is associated with file-name-1 and that is defined without
      *> the SOURCE phrase in the RECORD KEY clause or ALTERNATE RECORD
      *> KEY clause".  -> cite.py OK §2002:14.8.37.2 SR6
      *> G1 begins at the leftmost character position of S1, the first
      *> data-name-2 of the alternate key SK, which IS defined with the
      *> SOURCE phrase; it is no prime or alternate record key itself
      *> (SR6 a), and the only key it lines up with is a SOURCE key, so
      *> b) 1. excludes it. Same class, category and usage and shorter
      *> than SK, so only the SOURCE exclusion is violated. Refused at
      *> the START statement (COBOL0862). With KEY IS = SK, or a key
      *> without SOURCE under G1, it is the legal shape
      *> pb354_start_generic_key_ok shows accepted at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRCG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "G2SRCG.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS SK SOURCE IS S1 S2
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 PK  PIC X(3).
          05 S1  PIC X(2).
          05 S2  PIC X(2).
       01 IX-GEN.
          05 FILLER PIC X(3).
          05 G1  PIC X(1).
          05 FILLER PIC X(3).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT IXF
           MOVE "A" TO G1
           START IXF KEY IS = G1
               INVALID KEY DISPLAY "INV"
           END-START
           CLOSE IXF
           STOP RUN.
