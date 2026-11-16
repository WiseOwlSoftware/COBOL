      *>--- conformance-case
      *> id: g85_f22_07
      *> rules: [1985-GR-IX.4.9.4-7, 1985-GR-IX.4.9.4-11]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR7 - WRITE releases a logical record to the
      *> operating system (all three records are read back).
      *> §1985:IX.4.9.4 GR11 - the MSCS uses the contents of the
      *> record keys so that a record may later be accessed by any
      *> of its record keys: records are reached by the prime key
      *> and by the alternate key, and a sequential pass on the
      *> alternate key returns alternate-key order. A leg fails if
      *> a record is lost or a key does not reach its record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F2207.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G85F2207.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS PK
               ALTERNATE RECORD KEY IS AK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F
           LABEL RECORDS ARE STANDARD.
       01  R.
           05  PK PIC X(3).
           05  AK PIC X(3).
           05  D  PIC X(4).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "K02" TO PK. MOVE "ZZZ" TO AK. MOVE "TWO " TO D.
           WRITE R INVALID KEY DISPLAY "INVALID K02".
           MOVE "K01" TO PK. MOVE "MMM" TO AK. MOVE "ONE " TO D.
           WRITE R INVALID KEY DISPLAY "INVALID K01".
           MOVE "K03" TO PK. MOVE "AAA" TO AK. MOVE "THRE" TO D.
           WRITE R INVALID KEY DISPLAY "INVALID K03".
           DISPLAY "WRITE " FS.
           CLOSE F.
           OPEN INPUT F.
           MOVE "K01" TO PK.
           READ F INVALID KEY DISPLAY "NO K01".
           DISPLAY "PK K01 " AK " " D.
           MOVE "ZZZ" TO AK.
           READ F KEY IS AK INVALID KEY DISPLAY "NO ZZZ".
           DISPLAY "AK ZZZ " PK " " D.
           MOVE "AAA" TO AK.
           READ F KEY IS AK INVALID KEY DISPLAY "NO AAA".
           DISPLAY "AK AAA " PK " " D.
           MOVE LOW-VALUES TO AK.
           START F KEY IS NOT LESS THAN AK
               INVALID KEY DISPLAY "NO START".
       P2.
           READ F NEXT RECORD AT END GO TO P3.
           DISPLAY "SEQ " AK " " PK.
           GO TO P2.
       P3.
           CLOSE F.
           STOP RUN.
