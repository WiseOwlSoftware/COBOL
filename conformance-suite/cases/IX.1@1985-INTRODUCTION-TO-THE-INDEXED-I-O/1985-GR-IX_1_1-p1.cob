      *>--- conformance-case
      *> id: 1985-GR-IX_1_1-p1
      *> rules: [1985-GR-IX.1.1-p1, 1985-GR-IX.1.2-p1, 1985-GR-IX.1.2-p2, 1985-GR-IX.1.3.1-p1, 1985-GR-IX.1.3.2-p1, 1985-GR-IX.1.3.3-p1, 1985-GR-IX.1.3.4-p1, 1985-GR-IX.1.3.4-p2, 1985-GR-IX.1.3.4-p5, 1985-GR-IX.1.3.5-p1, 1985-GR-IX.1.3.5-p2, 1985-GR-IX.1.3.5-p3, 1985-GR-IX.1.3.6-p1, 1985-GR-IX.2.3.1-p1, 1985-GR-IX.2.8.1-p1, 1985-GR-IX.3.2.1-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.1.1 p1; §1985:IX.1.2 p1; §1985:IX.1.2 p2;
      *> §1985:IX.1.3.1 p1; §1985:IX.1.3.2 p1; §1985:IX.1.3.3 p1;
      *> §1985:IX.1.3.4 p1; §1985:IX.1.3.4 p2; §1985:IX.1.3.4 p5;
      *> §1985:IX.1.3.5 p1; §1985:IX.1.3.5 p2; §1985:IX.1.3.5 p3;
      *> §1985:IX.1.3.6 p1; §1985:IX.2.3.1 p1; §1985:IX.2.8.1 p1;
      *> §1985:IX.3.2.1 p1 (INTRODUCTION TO THE INDEXED I-O MODULE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-14.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * s-14 probe p02: indexed file at 85 -- organization, access
      * order, duplicates in arrival order, file position indicator,
      * I-O status timing, USE only on unsuccessful completion,
      * invalid key (file not affected), at end, I-O-CONTROL, the
      * file control entry, the FD and declaratives.
      * Expected output in p02_indexed.out (from the 1985 text).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P02IDX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "S14IDXA"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K
               ALTERNATE RECORD KEY IS AK WITH DUPLICATES
               FILE STATUS IS FS.
           SELECT IX2 ASSIGN TO "S14IDXB"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS K2.
       I-O-CONTROL.
           RERUN ON SYSRR EVERY 100 RECORDS OF IX
           SAME AREA FOR IX IX2.
       DATA DIVISION.
       FILE SECTION.
       FD  IX
           RECORD CONTAINS 12 CHARACTERS
           LABEL RECORDS ARE STANDARD
           DATA RECORD IS IX-REC.
       01  IX-REC.
           05  K           PIC X(4).
           05  AK          PIC X(2).
           05  D           PIC X(6).
       FD  IX2
           LABEL RECORDS ARE STANDARD.
       01  IX2-REC.
           05  K2          PIC X(4).
           05  FILLER      PIC X(8).
       WORKING-STORAGE SECTION.
       01  FS              PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX.
       IX-ERR-P.
           DISPLAY "USE " FS.
       END DECLARATIVES.
       MAIN SECTION.
       M-1.
           OPEN OUTPUT IX.
           MOVE "0003BBTHREE " TO IX-REC. WRITE IX-REC.
           DISPLAY "W3 " FS.
           MOVE "0001AAONE   " TO IX-REC. WRITE IX-REC.
           DISPLAY "W1 " FS.
           MOVE "0002BBTWO   " TO IX-REC. WRITE IX-REC.
           DISPLAY "W2 " FS.
           MOVE "0001ZZDUP   " TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "IK-WRITE " FS.
           MOVE "0004CCFOUR  " TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "NO"
               NOT INVALID KEY DISPLAY "NIK " FS.
           CLOSE IX.
           OPEN INPUT IX.
           PERFORM 5 TIMES
               READ IX NEXT RECORD
                   AT END DISPLAY "AT-END " FS
                   NOT AT END DISPLAY "SEQ " IX-REC
               END-READ
           END-PERFORM.
           CLOSE IX.
           OPEN I-O IX.
           MOVE "BB" TO AK.
           START IX KEY IS = AK.
           READ IX NEXT RECORD. DISPLAY "ALT " K " " FS.
           READ IX NEXT RECORD. DISPLAY "ALT " K " " FS.
           READ IX NEXT RECORD. DISPLAY "ALT " K " " FS.
           MOVE "AA" TO AK.
           READ IX KEY IS AK. DISPLAY "BY-AK " K " " FS.
           MOVE "0009" TO K.
           READ IX.
           DISPLAY "AFTER-USE " FS.
           MOVE "0009XXNINE  " TO IX-REC.
           REWRITE IX-REC INVALID KEY DISPLAY "IK-REWRITE " FS.
           READ IX INVALID KEY DISPLAY "STILL-ABSENT " FS.
           MOVE "0008" TO K.
           DELETE IX RECORD INVALID KEY DISPLAY "IK-DELETE " FS.
           MOVE "0009" TO K.
           START IX KEY IS > K INVALID KEY DISPLAY "IK-START " FS.
           MOVE "0001" TO K.
           START IX KEY IS = K.
           READ IX NEXT RECORD. DISPLAY "FPI " IX-REC.
           MOVE "ONE-RW" TO D.
           REWRITE IX-REC. DISPLAY "RW " FS.
           READ IX NEXT RECORD. DISPLAY "FPI " IX-REC.
           DELETE IX RECORD. DISPLAY "DEL " FS.
           READ IX NEXT RECORD. DISPLAY "FPI " IX-REC.
           MOVE "0001" TO K.
           READ IX. DISPLAY "GOT " IX-REC.
           MOVE "0002" TO K.
           READ IX INVALID KEY DISPLAY "DELETED " FS.
           CLOSE IX.
           STOP RUN.
