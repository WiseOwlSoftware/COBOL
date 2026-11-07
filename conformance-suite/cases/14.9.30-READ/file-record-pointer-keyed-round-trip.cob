*>--- conformance-case
*> id: file-record-pointer-keyed-round-trip
*> rules: [GR-14.9.30.4-13, GR-13.18.60.4-23, DOC-A.1-216]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4838 - an INDEXED and a RELATIVE file whose record is a strongly-typed group (13.18.60.3 SR14) holding a
      *> data-pointer: the pointer rides the file as its 8-position token (DOC-A.1-216) and a READ, a READ KEY and a REWRITE
      *> restore it. 14.9.30.4 GR13 c): "If no exception condition exists, the record is made available in the record area" - the
      *> record as the file holds it, so the pointer read equals the pointer written (8.8.4.2.16) and the based item reached
      *> through it is the item addressed when the record was written, also after a REWRITE replaced the pointer and a READ
      *> by KEY found the record again. The pointer leaf follows the key in the record, so the key window is unaffected.
      *> (cite.py --check OK for 14.9.30.4 13).) The introducing edition is COBOL-2002 (the TYPEDEF clause, 13.18.58).
      *> Expected: the indexed record 05 comes back with AAAA, the REWRITE makes the next READ KEY give BBBB; the relative
      *> record comes back with its key 07 and BBBB; a READ KEY for an absent key leaves status 23 and the record area as it was.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILERECPTRKEYED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "filerecptrkeyed.ix"
              ORGANIZATION INDEXED ACCESS DYNAMIC
              RECORD KEY IS KK OF IREC FILE STATUS FS.
           SELECT RL ASSIGN TO "filerecptrkeyed.rl"
              ORGANIZATION RELATIVE ACCESS SEQUENTIAL
              FILE STATUS FS.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IREC TYPE T-IX.
       FD RL.
       01 RREC TYPE T-REC.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 TA PIC 9(2).
          05 TP USAGE POINTER.
       01 T-IX IS TYPEDEF STRONG.
          05 KK PIC XX.
          05 IP USAGE POINTER.
          05 IQ USAGE POINTER OCCURS 2.
       01 FS PIC XX.
       01 X PIC X(4) VALUE "AAAA".
       01 Y PIC X(4) VALUE "BBBB".
       01 BX PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT IX
           MOVE "05" TO KK OF IREC
           SET IP OF IREC TO ADDRESS OF X
           SET IQ OF IREC(2) TO ADDRESS OF Y
           WRITE IREC
           DISPLAY "WRITE " FS
           CLOSE IX
           OPEN I-O IX
           MOVE "05" TO KK OF IREC
           READ IX KEY KK OF IREC
           DISPLAY "READ " FS
           SET ADDRESS OF BX TO IP OF IREC
           DISPLAY BX
           IF IQ OF IREC(1) = NULL
              DISPLAY "IQ1 NULL"
           END-IF
           SET ADDRESS OF BX TO IQ OF IREC(2)
           DISPLAY BX
           SET IP OF IREC TO ADDRESS OF Y
           REWRITE IREC
           DISPLAY "REWRITE " FS
           MOVE "05" TO KK OF IREC
           READ IX KEY KK OF IREC
           SET ADDRESS OF BX TO IP OF IREC
           DISPLAY BX
           MOVE "99" TO KK OF IREC
           READ IX KEY KK OF IREC
           DISPLAY "ABSENT " FS
           SET ADDRESS OF BX TO IP OF IREC
           DISPLAY KK OF IREC " " BX
           CLOSE IX
           OPEN OUTPUT RL
           SET TP OF RREC TO ADDRESS OF Y
           MOVE 7 TO TA OF RREC
           WRITE RREC
           DISPLAY "WRITE " FS
           CLOSE RL
           OPEN INPUT RL
           INITIALIZE RREC
           READ RL
           DISPLAY "READ " FS
           SET ADDRESS OF BX TO TP OF RREC
           DISPLAY TA OF RREC " " BX
           CLOSE RL
           STOP RUN.
