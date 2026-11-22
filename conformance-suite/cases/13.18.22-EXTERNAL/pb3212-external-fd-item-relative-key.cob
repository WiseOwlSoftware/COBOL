      *>--- conformance-case
      *> id: pb3212-external-fd-item-relative-key
      *> rules: [GR-13.18.22.4-4, GR-12.4.5.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3212 - ISO 2023 13.18.22.4 GR4 b): "If the EXTERNAL
      *> clause is specified in a file description entry ... b) the data
      *> contained in all record description entries" of that file is
      *> external, so RK, an item of the EXTERNAL file KEYS-FILE's
      *> record, is an external data item, and 14.8.4.2's "the file
      *> status, linage and relative key data items shall be external
      *> data items" admits it as the RELATIVE KEY of the EXTERNAL file
      *> REL-FILE (12.4.5.3 GR1 h).  Why each leg can fail: the
      *> corresponding-external-item identity asked only the record's own
      *> EXTERNAL clause and type, so the program was refused COBOL1624;
      *> a key read from other storage would fetch the wrong record or
      *> take INVALID KEY.  Expected: record 5 then record 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3212A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KEYS-FILE ASSIGN TO "pb3212k.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT REL-FILE ASSIGN TO "pb3212r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD KEYS-FILE IS EXTERNAL.
       01 KEYS-REC.
          05 RK PIC 9(4).
          05 FILLER PIC X(6).
       FD REL-FILE IS EXTERNAL.
       01 REL-REC PIC X(10).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT REL-FILE
           MOVE 2 TO RK
           MOVE "SECOND" TO REL-REC
           WRITE REL-REC INVALID KEY DISPLAY "WRITE 2 INVALID".
           MOVE 5 TO RK
           MOVE "FIFTH" TO REL-REC
           WRITE REL-REC INVALID KEY DISPLAY "WRITE 5 INVALID".
           CLOSE REL-FILE
           OPEN INPUT REL-FILE
           MOVE 5 TO RK
           READ REL-FILE INVALID KEY DISPLAY "READ 5 INVALID".
           DISPLAY REL-REC
           MOVE 2 TO RK
           READ REL-FILE INVALID KEY DISPLAY "READ 2 INVALID".
           DISPLAY REL-REC
           CLOSE REL-FILE
           STOP RUN.
