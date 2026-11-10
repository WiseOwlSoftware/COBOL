      *>--- conformance-case
      *> id: 1985-GR-VIII_1_3_4-L2_4
      *> rules: [1985-GR-VIII.1.3.4-L2.4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GRL2.4 (LANGUAGE CONCEPTS.). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELST3X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO ABSENTA
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO ABSENTB
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FC ASSIGN TO ABSENTC
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-C.
           SELECT FL ASSIGN TO LOCKED
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-L.
       DATA DIVISION.
       FILE SECTION.
       FD  FA.
       01  RA PIC X(10).
       FD  FB.
       01  RB PIC X(10).
       FD  FC.
       01  RC PIC X(10).
       FD  FL.
       01  RL PIC X(10).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX VALUE "00".
       01  ST-B PIC XX VALUE "00".
       01  ST-C PIC XX VALUE "00".
       01  ST-L PIC XX VALUE "00".
       PROCEDURE DIVISION.
       DECLARATIVES.
       DA SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FA.
       DA-P.
           DISPLAY "INPUT ABSENT " ST-A.
           OPEN I-O FB.
           DISPLAY "USE PROCEDURE OF FB NOT EXECUTED".
           STOP RUN.
       DB SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FB.
       DB-P.
           DISPLAY "I-O ABSENT " ST-B.
           OPEN EXTEND FC.
           DISPLAY "USE PROCEDURE OF FC NOT EXECUTED".
           STOP RUN.
       DC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FC.
       DC-P.
           DISPLAY "EXTEND ABSENT " ST-C.
           OPEN INPUT FL.
           DISPLAY "USE PROCEDURE OF FL NOT EXECUTED".
           STOP RUN.
       DL SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FL.
       DL-P.
           DISPLAY "CLOSED WITH LOCK " ST-L.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT FL.
           CLOSE FL WITH LOCK.
           DISPLAY "LOCK " ST-L.
           OPEN INPUT FA.
           DISPLAY "USE PROCEDURE OF FA NOT EXECUTED".
           STOP RUN.
