      *>--- conformance-case
      *> id: pb5270_from_into_own_record_area_2014
      *> rules: [GR-14.9.30.4-4, GR-14.9.51.4-5, GR-14.9.35.4-7, GR-14.9.32.4-4, GR-14.9.34.4-5]
      *> accept: [2014]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5270 - ISO/IEC 1989:2014 lifts the 1985 and 2002 syntax rules that a FROM or INTO operand shall not be the
      *>   file's record area: 2014:E.3 lists each of the five phrases as no longer prohibited (item 21 READ INTO, 24 RELEASE
      *>   FROM, 25 RETURN INTO, 26 REWRITE FROM, 38 WRITE FROM: "Identifier-1 may now reference the same storage area as
      *>   record-name-1"), and 2023 keeps the model (2023:14.6.10, overlapping operands). The record's content after each
      *>   statement is undefined, so the program prints only what IS defined: each statement's I-O status, and that the
      *>   SORT returned its one record. 1985 and 2002 refuse each phrase (conformance:negative/pb5077-*-own-record-area).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5270P1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5270P1.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS.
           SELECT TF ASSIGN TO "PB5270P1.SRT".
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       SD  TF.
       01  TR.
           05  TK PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       01  N  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN-SECT SECTION.
       MAIN-PARA.
           OPEN OUTPUT SF.
           MOVE "ABCD" TO SREC.
           WRITE SREC FROM SREC.
           DISPLAY "WRITE FROM " FS.
           CLOSE SF.
           OPEN I-O SF.
           READ SF INTO SREC AT END DISPLAY "UNEXPECTED END".
           DISPLAY "READ INTO " FS.
           REWRITE SREC FROM SREC.
           DISPLAY "REWRITE FROM " FS.
           CLOSE SF.
           SORT TF ASCENDING KEY TK
               INPUT PROCEDURE IS IN-SECT
               OUTPUT PROCEDURE IS OUT-SECT.
           DISPLAY "RETURNED " N.
           STOP RUN.
       IN-SECT SECTION.
       IN-PARA.
           MOVE "BBBB" TO TK.
           RELEASE TR FROM TR.
       OUT-SECT SECTION.
       OUT-PARA.
           RETURN TF INTO TR AT END GO TO OUT-EXIT.
           ADD 1 TO N.
           GO TO OUT-PARA.
       OUT-EXIT.
           EXIT.
