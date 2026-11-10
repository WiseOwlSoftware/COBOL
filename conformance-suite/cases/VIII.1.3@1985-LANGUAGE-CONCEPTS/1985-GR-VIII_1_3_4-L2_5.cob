      *>--- conformance-case
      *> id: 1985-GR-VIII_1_3_4-L2_5
      *> rules: [1985-GR-VIII.1.3.4-L2.5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.1.3.4 GRL2.5 (LANGUAGE CONCEPTS.). An output test:
      *> the program must translate and run at 85 and DISPLAY exactly
      *> the lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RELST4X.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO REL4A
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-A.
           SELECT FB ASSIGN TO REL4B
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-B.
           SELECT FC ASSIGN TO REL4C
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-C.
           SELECT FD-V ASSIGN TO REL4D
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-D.
           SELECT FE ASSIGN TO REL4E
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-E.
           SELECT FF ASSIGN TO REL4F
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-F.
           SELECT FG ASSIGN TO REL4G
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-G.
           SELECT FH ASSIGN TO REL4H
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST-H.
       DATA DIVISION.
       FILE SECTION.
       FD  FA.
       01  REC-A PIC X(6).
       FD  FB.
       01  REC-B PIC X(6).
       FD  FC.
       01  REC-C PIC X(6).
       FD  FD-V
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS
               DEPENDING ON LEN-D.
       01  REC-D PIC X(6).
       FD  FE.
       01  REC-E PIC X(6).
       FD  FF.
       01  REC-F PIC X(6).
       FD  FG.
       01  REC-G PIC X(6).
       FD  FH.
       01  REC-H PIC X(6).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX VALUE "00".
       01  ST-B PIC XX VALUE "00".
       01  ST-C PIC XX VALUE "00".
       01  ST-D PIC XX VALUE "00".
       01  ST-E PIC XX VALUE "00".
       01  ST-F PIC XX VALUE "00".
       01  ST-G PIC XX VALUE "00".
       01  ST-H PIC XX VALUE "00".
       01  LEN-D PIC 9(2) VALUE 6.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DCL-A SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FA.
       DCL-A-P.
           DISPLAY "OPEN OF AN OPEN FILE " ST-A.
           CLOSE FB.
           DISPLAY "USE PROCEDURE OF FB NOT EXECUTED".
           STOP RUN.
       DCL-B SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FB.
       DCL-B-P.
           DISPLAY "CLOSE OF A CLOSED FILE " ST-B.
           MOVE "CHANGE" TO REC-C.
           REWRITE REC-C.
           DISPLAY "USE PROCEDURE OF FC NOT EXECUTED".
           STOP RUN.
       DCL-C SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FC.
       DCL-C-P.
           DISPLAY "REWRITE WITH NO READ " ST-C.
           MOVE 1 TO LEN-D.
           MOVE "X" TO REC-D.
           WRITE REC-D.
           DISPLAY "USE PROCEDURE OF FD-V NOT EXECUTED".
           STOP RUN.
       DCL-D SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FD-V.
       DCL-D-P.
           DISPLAY "RECORD TOO SMALL " ST-D.
           READ FE.
           DISPLAY "USE PROCEDURE OF FE NOT EXECUTED".
           STOP RUN.
       DCL-E SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FE.
       DCL-E-P.
           DISPLAY "READ AFTER AT END " ST-E.
           READ FF.
           DISPLAY "USE PROCEDURE OF FF NOT EXECUTED".
           STOP RUN.
       DCL-F SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FF.
       DCL-F-P.
           DISPLAY "READ OF A CLOSED FILE " ST-F.
           MOVE "RECORD" TO REC-G.
           WRITE REC-G.
           DISPLAY "USE PROCEDURE OF FG NOT EXECUTED".
           STOP RUN.
       DCL-G SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FG.
       DCL-G-P.
           DISPLAY "WRITE TO AN INPUT FILE " ST-G.
           DELETE FH RECORD.
           DISPLAY "USE PROCEDURE OF FH NOT EXECUTED".
           STOP RUN.
       DCL-H SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON FH.
       DCL-H-P.
           DISPLAY "DELETE FROM AN INPUT FILE " ST-H.
           STOP RUN.
       END DECLARATIVES.
       MAIN-SECTION SECTION.
       MAIN-PARA.
           OPEN OUTPUT FC.
           MOVE "RECORD" TO REC-C.
           WRITE REC-C.
           CLOSE FC.
           OPEN I-O FC.
           OPEN OUTPUT FD-V.
           OPEN OUTPUT FE.
           CLOSE FE.
           OPEN INPUT FE.
           READ FE AT END DISPLAY "AT END " ST-E.
           OPEN OUTPUT FG.
           CLOSE FG.
           OPEN INPUT FG.
           OPEN OUTPUT FH.
           MOVE "RECORD" TO REC-H.
           WRITE REC-H.
           CLOSE FH.
           OPEN INPUT FH.
           READ FH AT END DISPLAY "FH IS EMPTY".
           DISPLAY "READ " ST-H.
           OPEN OUTPUT FA.
           OPEN INPUT FA.
           DISPLAY "USE PROCEDURE OF FA NOT EXECUTED".
           STOP RUN.
