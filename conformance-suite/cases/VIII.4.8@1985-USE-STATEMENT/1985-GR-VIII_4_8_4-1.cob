      *>--- conformance-case
      *> id: 1985-GR-VIII_4_8_4-1
      *> rules: [1985-GR-VIII.4.8.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.8.4 GR1 (THE USE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USEMAIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO RELA
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FA
           LABEL RECORDS ARE STANDARD.
       01  FA-REC PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       MAIN-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       MAIN-ERR-PARA.
           DISPLAY "MAIN DECL".
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT FA.
           MOVE "RECORD1" TO FA-REC.
           WRITE FA-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE FA.
           OPEN INPUT FA.
           READ FA.
           DISPLAY "MAIN READ 1".
           READ FA.
           DISPLAY "MAIN READ 2".
           CLOSE FA.
           CALL "USEIN".
           CALL "USESUB".
           OPEN INPUT FA.
           READ FA.
           DISPLAY "MAIN READ 3".
           READ FA.
           DISPLAY "MAIN READ 4".
           CLOSE FA.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USEIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FB ASSIGN TO RELB
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FB
           LABEL RECORDS ARE STANDARD.
       01  FB-REC PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       IN-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       IN-ERR-PARA.
           DISPLAY "INNER DECL".
       END DECLARATIVES.
       IN-SECT SECTION.
       IN-PARA.
           OPEN OUTPUT FB.
           MOVE "RECORD1" TO FB-REC.
           WRITE FB-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE FB.
           OPEN INPUT FB.
           READ FB.
           DISPLAY "INNER READ 1".
           READ FB.
           DISPLAY "INNER READ 2".
           CLOSE FB.
           EXIT PROGRAM.
       END PROGRAM USEIN.
       END PROGRAM USEMAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. USESUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FC ASSIGN TO RELC
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FC
           LABEL RECORDS ARE STANDARD.
       01  FC-REC PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       SUB-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       SUB-ERR-PARA.
           DISPLAY "SUB DECL".
       END DECLARATIVES.
       SUB-SECT SECTION.
       SUB-PARA.
           OPEN OUTPUT FC.
           MOVE "RECORD1" TO FC-REC.
           WRITE FC-REC
               INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE FC.
           OPEN INPUT FC.
           READ FC.
           DISPLAY "SUB READ 1".
           READ FC.
           DISPLAY "SUB READ 2".
           CLOSE FC.
           EXIT PROGRAM.
       END PROGRAM USESUB.
