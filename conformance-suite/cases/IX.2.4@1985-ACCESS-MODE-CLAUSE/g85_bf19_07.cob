      *>--- conformance-case
      *> id: g85_bf19_07
      *> rules: [1985-GR-IX.2.4.4-1, 1985-GR-IX.2.4.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.4.4 GR1 - no ACCESS MODE clause: sequential access.
      *>   cite.py: OK §1985:IX.2.4.4 GR1 "If the ACCESS MODE clause is
      *>   not specified, sequential access is assumed".
      *> §1985:IX.2.4.4 GR2 - sequential access reads an indexed file
      *>   ascending within the key of reference. cite.py: OK
      *>   §1985:IX.2.4.4 GR2 "ascending within a given key of reference
      *>   according to the collating sequence of the file".
      *> Derivation: IXW (ACCESS DYNAMIC) writes C1, A3, B2 (prime key the
      *>   letter, alternate key the digit). IX has no ACCESS clause, so it
      *>   is sequential (GR1); OPEN INPUT makes the prime key the key of
      *>   reference (§1985:IX.4.4.4 GR10 "the prime record key is
      *>   established as the key of reference"), so READ ascends on it
      *>   (GR2): IX:A3, IX:B2, IX:C1, IX:END. START on IX-ALT makes the
      *>   alternate key the key of reference for the READs that follow
      *>   (§1985:IX.4.7.4 GR10 "the key of reference is also used for
      *>   subsequent sequential READ statements"): ascending on the digit,
      *>   IX:C1, IX:B2, IX:A3, IX:END. A<B<C and 1<2<3 in any native
      *>   collating sequence.
      *>   IY has no ACCESS clause: sequential, so WRITEs must ascend
      *>   (§1985:IX.4.9.4 GR14 "records must be released to the mass
      *>   storage control system (MSCS) in ascending order of prime record
      *>   key values"); C3 then A1 gives 00 then 21 (§1985:IX.1.3.4
      *>   GRL2.3 "the ascending sequence requirements for successive
      *>   record key values are violated"), with the INVALID KEY phrase
      *>   executed. Under random or dynamic access A1 would be accepted.
      *>   Lines 1-8 pin GR2 (and GR1); lines 9-10 pin GR1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1907.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXW ASSIGN TO "BF1907.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXW-KEY
               ALTERNATE RECORD KEY IS IXW-ALT
               FILE STATUS IS IW-ST.
           SELECT IX ASSIGN TO "BF1907.DAT"
               ORGANIZATION IS INDEXED
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               FILE STATUS IS IX-ST.
           SELECT IY ASSIGN TO "BF1907Y.DAT"
               ORGANIZATION IS INDEXED
               RECORD KEY IS IY-KEY
               FILE STATUS IS IY-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IXW.
       01  IXW-REC.
           05  IXW-KEY PIC X.
           05  IXW-ALT PIC X.
       FD  IX.
       01  IX-REC.
           05  IX-KEY PIC X.
           05  IX-ALT PIC X.
       FD  IY.
       01  IY-REC.
           05  IY-KEY PIC X.
           05  IY-VAL PIC X.
       WORKING-STORAGE SECTION.
       01  IW-ST PIC XX.
       01  IX-ST PIC XX.
       01  IY-ST PIC XX.
       01  IY-ST1 PIC XX.
       01  WS-EOF PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
      * Build the file out of key order through dynamic access.
           OPEN OUTPUT IXW.
           MOVE "C1" TO IXW-REC.
           WRITE IXW-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE "A3" TO IXW-REC.
           WRITE IXW-REC INVALID KEY DISPLAY "BAD WRITE".
           MOVE "B2" TO IXW-REC.
           WRITE IXW-REC INVALID KEY DISPLAY "BAD WRITE".
           CLOSE IXW.
      * IX has no ACCESS MODE clause: sequential (GR1), ascending
      * on the prime key, the key of reference after OPEN (GR2).
           OPEN INPUT IX.
           MOVE "N" TO WS-EOF.
           PERFORM READ-IX UNTIL WS-EOF = "Y".
      * START makes the alternate key the key of reference.
           MOVE LOW-VALUE TO IX-ALT.
           START IX KEY IS NOT LESS THAN IX-ALT
               INVALID KEY DISPLAY "BAD START".
           MOVE "N" TO WS-EOF.
           PERFORM READ-IX UNTIL WS-EOF = "Y".
           CLOSE IX.
      * IY has no ACCESS MODE clause: sequential WRITEs must ascend.
           OPEN OUTPUT IY.
           MOVE "C3" TO IY-REC.
           WRITE IY-REC INVALID KEY DISPLAY "IY:INVALID C3".
           MOVE IY-ST TO IY-ST1.
           MOVE "A1" TO IY-REC.
           WRITE IY-REC INVALID KEY DISPLAY "IY:INVALID A1".
           DISPLAY "IY:" IY-ST1 " " IY-ST.
           CLOSE IY.
           STOP RUN.
       READ-IX.
           READ IX AT END
               MOVE "Y" TO WS-EOF
               DISPLAY "IX:END".
           IF WS-EOF = "N"
               DISPLAY "IX:" IX-REC.
