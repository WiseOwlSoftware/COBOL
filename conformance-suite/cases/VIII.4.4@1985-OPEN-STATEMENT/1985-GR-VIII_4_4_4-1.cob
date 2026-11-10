      *>--- conformance-case
      *> id: 1985-GR-VIII_4_4_4-1
      *> rules: [1985-GR-VIII.4.4.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.4.4 GR1 (THE OPEN STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. OPENTBL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "T1F1"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S1.
           SELECT OPTIONAL F2 ASSIGN TO "T1F2"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S2.
           SELECT OPTIONAL F3 ASSIGN TO "T1F3"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S3.
           SELECT OPTIONAL F4 ASSIGN TO "T1F4"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S4.
           SELECT F5 ASSIGN TO "T1F5"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S5.
           SELECT F6 ASSIGN TO "T1F6"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S6.
           SELECT F7 ASSIGN TO "T1F7"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S7.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(10).
       FD  F2
           LABEL RECORDS ARE STANDARD.
       01  R2 PIC X(10).
       FD  F3
           LABEL RECORDS ARE STANDARD.
       01  R3 PIC X(10).
       FD  F4
           LABEL RECORDS ARE STANDARD.
       01  R4 PIC X(10).
       FD  F5
           LABEL RECORDS ARE STANDARD.
       01  R5 PIC X(10).
       FD  F6
           LABEL RECORDS ARE STANDARD.
       01  R6 PIC X(10).
       FD  F7
           LABEL RECORDS ARE STANDARD.
       01  R7 PIC X(10).
       WORKING-STORAGE SECTION.
       01  S1 PIC XX VALUE "99".
       01  S2 PIC XX VALUE "99".
       01  S3 PIC XX VALUE "99".
       01  S4 PIC XX VALUE "99".
       01  S5 PIC XX VALUE "99".
       01  S6 PIC XX VALUE "99".
       01  S7 PIC XX VALUE "99".
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE5 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F5.
       USE5-P.
           DISPLAY "INPUT UNAVAILABLE " S5.
           OPEN I-O F6.
           DISPLAY "USE PROCEDURE OF F6 NOT EXECUTED".
           STOP RUN.
       USE6 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F6.
       USE6-P.
           DISPLAY "I-O UNAVAILABLE " S6.
           OPEN EXTEND F7.
           DISPLAY "USE PROCEDURE OF F7 NOT EXECUTED".
           STOP RUN.
       USE7 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F7.
       USE7-P.
           DISPLAY "EXTEND UNAVAILABLE " S7.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           DISPLAY "OUTPUT UNAVAILABLE " S1.
           MOVE "AAAAAAAAAA" TO R1.
           WRITE R1
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE F1.
           OPEN OUTPUT F1.
           DISPLAY "OUTPUT AVAILABLE " S1.
           CLOSE F1.
           OPEN INPUT F1.
           DISPLAY "INPUT AVAILABLE " S1.
           READ F1
               AT END DISPLAY "NO RECORDS".
           CLOSE F1.
           OPEN EXTEND F1.
           DISPLAY "EXTEND AVAILABLE " S1.
           MOVE "BBBBBBBBBB" TO R1.
           WRITE R1
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE F1.
           OPEN I-O F1.
           DISPLAY "I-O AVAILABLE " S1.
           READ F1
               AT END DISPLAY "NO RECORDS".
           DISPLAY R1.
           CLOSE F1.
           OPEN INPUT F2.
           DISPLAY "INPUT OPTIONAL UNAVAILABLE " S2.
           READ F2
               AT END DISPLAY "FIRST READ AT END " S2.
           CLOSE F2.
           OPEN I-O F3.
           DISPLAY "I-O OPTIONAL UNAVAILABLE " S3.
           CLOSE F3.
           OPEN I-O F3.
           DISPLAY "I-O OPTIONAL AVAILABLE " S3.
           READ F3
               AT END DISPLAY "CREATED WITH NO RECORDS".
           CLOSE F3.
           OPEN EXTEND F4.
           DISPLAY "EXTEND OPTIONAL UNAVAILABLE " S4.
           MOVE "CCCCCCCCCC" TO R4.
           WRITE R4
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE F4.
           OPEN EXTEND F4.
           DISPLAY "EXTEND OPTIONAL AVAILABLE " S4.
           CLOSE F4.
           OPEN INPUT F4.
           DISPLAY "INPUT OPTIONAL AVAILABLE " S4.
           READ F4
               AT END DISPLAY "NO RECORDS".
           DISPLAY R4.
           CLOSE F4.
           OPEN INPUT F5.
           DISPLAY "USE PROCEDURE OF F5 NOT EXECUTED".
           STOP RUN.
