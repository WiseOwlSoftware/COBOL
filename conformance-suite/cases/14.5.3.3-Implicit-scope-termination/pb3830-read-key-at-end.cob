      *>--- conformance-case
      *> id: pb3830-read-key-at-end
      *> rules: [GR-14.5.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830, branch review R1-1. ISO 14.5.3.3: "Any phrase
      *> encountered is the next phrase of the most-recently preceding
      *> unterminated statement with which that phrase may be syntactically
      *> associated." (cite.py --check 14.5.3.3 -> OK.) A phrase may be
      *> associated with a statement only when a general format of the
      *> statement prints it beside everything the statement already wrote.
      *> READ ... KEY IS is Format 2 only (14.9.30.2), which prints no AT
      *> END phrase.
      *> No other statement is open, so the phrase may be syntactically
      *> associated with no statement: refused COBOL2269 (the
      *> statement-format-shape code) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830S2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXG ASSIGN TO "PB3830S2G.TMP"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXG-KEY.
           SELECT PRT ASSIGN TO "PB3830S2P.TMP"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  IXG.
       01  IXG-REC.
           05 IXG-KEY PIC X(4).
       FD  PRT.
       01  PRT-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  F    PIC X VALUE "-".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT IXG
           READ IXG KEY IS IXG-KEY
               AT END MOVE "E" TO F
           END-READ
           STOP RUN.
