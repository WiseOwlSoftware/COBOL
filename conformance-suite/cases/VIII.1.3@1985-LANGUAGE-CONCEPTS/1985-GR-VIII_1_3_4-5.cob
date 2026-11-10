      *>--- conformance-case
      *> id: 1985-GR-VIII_1_3_4-5
      *> rules: [1985-GR-VIII.1.3.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GR5 (LANGUAGE CONCEPTS.). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELLOG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO RELLOGA
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO RELLOGB
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-B.
       DATA DIVISION.
       FILE SECTION.
       FD  FA
           LABEL RECORDS ARE STANDARD.
       01  RA PIC X(10).
       FD  FB
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
               DEPENDING ON LEN-B.
       01  RB PIC X(6).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX VALUE "00".
       01  ST-B PIC XX VALUE "00".
       01  LEN-B PIC 9(2) VALUE 6.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DA SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FA.
       DA-P.
           DISPLAY "SEQUENCE " ST-A.
           MOVE 1 TO LEN-B.
           MOVE "X" TO RB.
           WRITE RB.
           DISPLAY "USE PROCEDURE OF FB NOT EXECUTED".
           STOP RUN.
       DB SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FB.
       DB-P.
           DISPLAY "LIMIT " ST-B.
           STOP RUN.
       END DECLARATIVES.
       MAIN-LOGIC SECTION.
       M1.
           OPEN OUTPUT FB.
           MOVE "ABCDEF" TO RB.
           WRITE RB.
           DISPLAY "IN LIMIT " ST-B.
           CLOSE FA.
           DISPLAY "USE PROCEDURE OF FA NOT EXECUTED".
           STOP RUN.
