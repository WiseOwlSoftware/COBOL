      *>--- conformance-case
      *> id: g85_bf31_04
      *> rules: [1985-GR-X.1.3.6-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.6 GR1 - "The data content of an external data
      *> record may be referenced from any program provided that
      *> program has described that data record." MAIN, A and B each
      *> describe external BF31-SHARE (7 characters, X.4.5.4 GR2), each
      *> with its own subordinate names; B describes it as one PIC
      *> X(7). MAIN sets MAIN/100; A must see "MAIN 100" and sets
      *> SUBA/101; MAIN then sees "SUBA 101"; B sees "SUBA101" and sets
      *> SUBB200; MAIN sees "SUBB 200". A private copy per program
      *> would leave A seeing spaces/zero and MAIN unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3104.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-SHARE EXTERNAL.
           05  BF31-MAIN-A PIC X(4).
           05  BF31-MAIN-N PIC 9(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "MAIN" TO BF31-MAIN-A.
           MOVE 100 TO BF31-MAIN-N.
           CALL "G85BF3104A".
           DISPLAY "MAIN SEES " BF31-MAIN-A " " BF31-MAIN-N.
           CALL "G85BF3104B".
           DISPLAY "MAIN SEES " BF31-MAIN-A " " BF31-MAIN-N.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3104A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-SHARE EXTERNAL.
           05  BF31-A-TEXT PIC X(4).
           05  BF31-A-NUM  PIC 9(3).
       PROCEDURE DIVISION.
       A-PARA.
           DISPLAY "A SEES " BF31-A-TEXT " " BF31-A-NUM.
           MOVE "SUBA" TO BF31-A-TEXT.
           ADD 1 TO BF31-A-NUM.
           EXIT PROGRAM.
       END PROGRAM G85BF3104A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3104B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  BF31-SHARE EXTERNAL PIC X(7).
       PROCEDURE DIVISION.
       B-PARA.
           DISPLAY "B SEES " BF31-SHARE.
           MOVE "SUBB200" TO BF31-SHARE.
           EXIT PROGRAM.
       END PROGRAM G85BF3104B.
       END PROGRAM G85BF3104.
