      *>--- conformance-case
      *> id: g85_bf19_06
      *> rules: [1985-GR-VIII.2.4.4-1, 1985-GR-VIII.2.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.2.4.4 GR1 - no ACCESS MODE clause: sequential access.
      *>   cite.py: OK §1985:VIII.2.4.4 GR1 "If the ACCESS MODE clause is
      *>   not specified, sequential access is assumed".
      *> §1985:VIII.2.4.4 GR2 - sequential access reads a relative file in
      *>   ascending relative record number of existing records.
      *>   cite.py: OK §1985:VIII.2.4.4 GR2 "the order of ascending
      *>   relative record numbers of existing records in the file".
      *> At 85 RELATIVE KEY is a phrase of the ACCESS MODE clause
      *>   (§1985:VIII.2.3.2 general format), so RL and RS, which omit the
      *>   clause, have no relative key.
      *> Derivation: RLW (ACCESS RANDOM) writes R5, R2, R9 at RRN 5, 2, 9.
      *>   RL has no ACCESS clause, so it is sequential (GR1) and a format 1
      *>   READ returns the existing records by ascending RRN (GR2): RL:R2,
      *>   RL:R5, RL:R9, then at end RL:END.
      *>   RS has no ACCESS clause: its WRITEs are sequential, the first
      *>   record getting RRN 1 and the next 2 (§1985:VIII.4.9.4 GR11 "The
      *>   first record has a relative record number of one"); RSR reads
      *>   RRN 2 then 1 at random: RS:02 W2, RS:01 W1. Lines 1-4 pin GR2
      *>   (and GR1); lines 5-6 pin GR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1906.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RLW ASSIGN TO "BF1906.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKW
               FILE STATUS IS RW-ST.
           SELECT RL ASSIGN TO "BF1906.DAT"
               ORGANIZATION IS RELATIVE
               FILE STATUS IS RL-ST.
           SELECT RS ASSIGN TO "BF1906S.DAT"
               ORGANIZATION IS RELATIVE
               FILE STATUS IS RS-ST.
           SELECT RSR ASSIGN TO "BF1906S.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RSK
               FILE STATUS IS RR-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  RLW.
       01  RLW-REC PIC X(2).
       FD  RL.
       01  RL-REC PIC X(2).
       FD  RS.
       01  RS-REC PIC X(2).
       FD  RSR.
       01  RSR-REC PIC X(2).
       WORKING-STORAGE SECTION.
       01  RW-ST PIC XX.
       01  RL-ST PIC XX.
       01  RS-ST PIC XX.
       01  RR-ST PIC XX.
       01  RKW PIC 99.
       01  RSK PIC 99.
       01  WS-EOF PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
      * Build the file out of order through random access.
           OPEN OUTPUT RLW.
           MOVE 5 TO RKW. MOVE "R5" TO RLW-REC.
           WRITE RLW-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE 2 TO RKW. MOVE "R2" TO RLW-REC.
           WRITE RLW-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE 9 TO RKW. MOVE "R9" TO RLW-REC.
           WRITE RLW-REC INVALID KEY DISPLAY "BAD WRITE".
           CLOSE RLW.
      * RL has no ACCESS MODE clause: sequential (GR1), and its
      * READs return the existing records by ascending RRN (GR2).
           OPEN INPUT RL.
           MOVE "N" TO WS-EOF.
           PERFORM READ-RL UNTIL WS-EOF = "Y".
           CLOSE RL.
      * RS has no ACCESS MODE clause: its WRITEs are sequential and
      * number the records 1, 2; RSR reads them back by RRN.
           OPEN OUTPUT RS.
           MOVE "W1" TO RS-REC.
           WRITE RS-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE "W2" TO RS-REC.
           WRITE RS-REC INVALID KEY DISPLAY "BAD WRITE".
           CLOSE RS.
           OPEN INPUT RSR.
           MOVE 2 TO RSK.
           READ RSR INVALID KEY DISPLAY "RS:NO 02".
           DISPLAY "RS:02 " RSR-REC.
           MOVE 1 TO RSK.
           READ RSR INVALID KEY DISPLAY "RS:NO 01".
           DISPLAY "RS:01 " RSR-REC.
           CLOSE RSR.
           STOP RUN.
       READ-RL.
           READ RL AT END
               MOVE "Y" TO WS-EOF
               DISPLAY "RL:END".
           IF WS-EOF = "N"
               DISPLAY "RL:" RL-REC.
