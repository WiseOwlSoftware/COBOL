      *>--- conformance-case
      *> id: g85_f16_06
      *> rules: [1985-GR-IX.4.5.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.5.4 GR8 - READ ... INTO on an indexed file is the
      *> READ without INTO followed by a MOVE of the current record
      *> to identifier-1. Legs:
      *>  L1  the subscript of WE (IX-SEQ) lies in the record, and is
      *>      evaluated after the record is read: A0013LONGDAT (SEQ 3)
      *>      goes to WE (3), not WE (1) set before the READ. The
      *>      record is in both the record area and WE (3).
      *>  L2  RECORD IS VARYING: the sender is the current record, its
      *>      size from the RECORD clause (6, §1985:VII.3.8.4 GR12a),
      *>      and the implied move is a group move, filled without
      *>      regard to the receiver's items (§1985:VI.6.19.4 GR5):
      *>      the edited
      *>      receiver XXBXXBXX gets B0021X and two spaces, unedited
      *>      (an elementary move would insert the B positions).
      *>  L3  the READ is at end (10): the implied MOVE does not occur
      *>      and WN keeps UNCHANGED.
      *>  L4  a random READ of an absent key is an invalid key (23):
      *>      again no implied MOVE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1606.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "g85f1606.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF
           LABEL RECORDS ARE STANDARD
           RECORD IS VARYING IN SIZE FROM 6 TO 12 CHARACTERS
               DEPENDING ON RLEN.
       01  IXREC.
           05 IX-KEY           PIC X(4).
           05 IX-SEQ           PIC 9.
           05 IX-REST          PIC X(7).
       WORKING-STORAGE SECTION.
       01  FS                  PIC XX.
       01  RLEN                PIC 99.
       01  WT.
           05 WE               PIC X(12) OCCURS 3 TIMES.
       01  WED                 PIC XXBXXBXX.
       01  WN                  PIC X(10).
       PROCEDURE DIVISION.
       MAIN-LOGIC SECTION.
       M-LOAD.
           OPEN OUTPUT IXF.
           MOVE "A0013LONGDAT" TO IXREC.
           MOVE 12 TO RLEN.
           WRITE IXREC INVALID KEY DISPLAY "W1 IK " FS.
           MOVE "B0021X" TO IXREC.
           MOVE 6 TO RLEN.
           WRITE IXREC INVALID KEY DISPLAY "W2 IK " FS.
           CLOSE IXF.
       M-L1.
           OPEN INPUT IXF.
           MOVE ALL "." TO WT.
           MOVE 1 TO IX-SEQ.
           READ IXF NEXT RECORD INTO WE (IX-SEQ)
               AT END DISPLAY "L1 AT END"
           END-READ.
           DISPLAY "L1 " FS " " RLEN " " IXREC.
           DISPLAY "L1 WT " WT.
       M-L2.
           MOVE ALL "." TO WED.
           READ IXF NEXT RECORD INTO WED
               AT END DISPLAY "L2 AT END"
           END-READ.
           DISPLAY "L2 " FS " " RLEN " [" WED "]".
       M-L3.
           MOVE "UNCHANGED" TO WN.
           READ IXF NEXT RECORD INTO WN
               AT END DISPLAY "L3 AT END " FS
           END-READ.
           DISPLAY "L3 [" WN "]".
       M-L4.
           MOVE "Z999" TO IX-KEY.
           READ IXF RECORD INTO WN
               INVALID KEY DISPLAY "L4 INVALID KEY " FS
           END-READ.
           DISPLAY "L4 [" WN "]".
           CLOSE IXF.
           STOP RUN.
