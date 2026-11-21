      *>--- conformance-case
      *> id: 1985-GR-VII_1_3_5-p1
      *> rules: [1985-GR-VII.1.3.5-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.1.3.5 p1 (LANGUAGE CONCEPTS). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-12.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P08IOS.
      * 1985 VII.1.3.5 p1: the I-O status is set during the I-O
      * statement, before its AT END imperative statement and before
      * an applicable USE procedure, and the FILE STATUS item shows it.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "P08F.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FSF.
           SELECT G ASSIGN TO "P08NOFILE.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FSG.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  RECF PIC X(4).
       FD  G.
       01  RECG PIC X(4).
       WORKING-STORAGE SECTION.
       01  FSF PIC XX VALUE "XX".
       01  FSG PIC XX VALUE "XX".
       PROCEDURE DIVISION.
       DECLARATIVES.
       GERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON G.
       GERR-1.
           DISPLAY "USE FSG=" FSG.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           OPEN OUTPUT F.
           DISPLAY "OPEN-OUT FSF=" FSF.
           MOVE "AAAA" TO RECF.
           WRITE RECF.
           DISPLAY "WRITE FSF=" FSF.
           CLOSE F.
           DISPLAY "CLOSE FSF=" FSF.
           OPEN INPUT F.
           READ F AT END DISPLAY "AT END 1 FSF=" FSF.
           DISPLAY "READ1 FSF=" FSF.
           READ F AT END DISPLAY "AT END 2 FSF=" FSF.
           CLOSE F.
           OPEN INPUT G.
           DISPLAY "AFTER OPEN G FSG=" FSG.
           STOP RUN.
