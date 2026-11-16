      *>--- conformance-case
      *> id: g85_bf29_01
      *> rules: [1985-GR-VIII.1.3.4-2, 1985-GR-VIII.1.3.4-L2.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      * §1985:VIII.1.3.4 GR2 - "A sequential READ statement was
      * unsuccessfully executed as a result of an at end condition": each
      * at end leg below takes AT END, skips NOT AT END, and leaves a status
      * whose first character is 1.
      * §1985:VIII.1.3.4 GRL2.2 - the three at end statuses of a relative
      * file: a.1 end of file reached -> 10 (A3); a.2 first sequential READ
      * of an absent OPTIONAL input file -> 10 (O1); b. relative record
      * number with more significant digits than the RELATIVE KEY item -> 14
      * (S2: record 12 against SK PIC 9).
      * Records 3 and 12 are written through a connector whose key is
      * PIC 99; RS (key PIC 9) and RA (key PIC 99) read the same file
      * sequentially. §1985:VIII.4.5.4 GR14 moves the record number to the
      * relative key on each successful READ (S1, A1, A2).
      * A leg fails if the overflow gives 00/10 instead of 14, if the
      * absent optional file reads as anything but 10, or if a successful
      * READ takes AT END.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2901.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RW ASSIGN TO "g85bf2901.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS WK
               FILE STATUS IS FS-W.
           SELECT RS ASSIGN TO "g85bf2901.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS SK
               FILE STATUS IS FS-S.
           SELECT RA ASSIGN TO "g85bf2901.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS AK
               FILE STATUS IS FS-A.
           SELECT OPTIONAL RO ASSIGN TO "g85bf2901n.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS FS-O.
       DATA DIVISION.
       FILE SECTION.
       FD  RW.
       01  WREC PIC X(5).
       FD  RS.
       01  SREC PIC X(5).
       FD  RA.
       01  AREC PIC X(5).
       FD  RO.
       01  OREC PIC X(5).
       WORKING-STORAGE SECTION.
       01  WK   PIC 99.
       01  SK   PIC 9.
       01  AK   PIC 99.
       01  FS-W PIC XX.
       01  FS-S PIC XX.
       01  FS-A PIC XX.
       01  FS-O PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RW.
           MOVE 3 TO WK.
           MOVE "REC03" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W3 INVALID " FS-W.
           MOVE 12 TO WK.
           MOVE "REC12" TO WREC.
           WRITE WREC INVALID KEY DISPLAY "W12 INVALID " FS-W.
           CLOSE RW.
           OPEN INPUT RS.
           READ RS AT END DISPLAY "S1 AT END " FS-S
               NOT AT END DISPLAY "S1 READ " SREC " " FS-S " " SK.
           READ RS AT END DISPLAY "S2 AT END " FS-S
               NOT AT END DISPLAY "S2 READ " SREC " " FS-S " " SK.
           CLOSE RS.
           OPEN INPUT RA.
           READ RA AT END DISPLAY "A1 AT END " FS-A
               NOT AT END DISPLAY "A1 READ " AREC " " FS-A " " AK.
           READ RA AT END DISPLAY "A2 AT END " FS-A
               NOT AT END DISPLAY "A2 READ " AREC " " FS-A " " AK.
           READ RA AT END DISPLAY "A3 AT END " FS-A
               NOT AT END DISPLAY "A3 READ " AREC " " FS-A " " AK.
           CLOSE RA.
           OPEN INPUT RO.
           DISPLAY "O0 OPEN " FS-O.
           READ RO AT END DISPLAY "O1 AT END " FS-O
               NOT AT END DISPLAY "O1 READ " FS-O.
           CLOSE RO.
           STOP RUN.
