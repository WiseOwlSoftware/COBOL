      *>--- conformance-case
      *> id: pb3212-external-fd-key-invalid-key
      *> rules: [GR-13.18.22.4-4, GR-12.4.5.3-1, GR-14.9.30.4-29]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3212 review R1-1 (c), the exception path of
      *> conformance:pb3212-external-fd-item-relative-key. RK is an
      *> item of the EXTERNAL file KEYS-FILE's record, so it is an
      *> external data item (ISO 2023 13.18.22.4 GR4 b) and may be the
      *> RELATIVE KEY of the EXTERNAL file REL-FILE (14.8.4.2; 12.4.5.3
      *> GR1 h); FS is an EXTERNAL working-storage item, since a FILE
      *> STATUS item is not in the file section (12.4.5.8.3 SR2).
      *> Only record 2 is written.
      *> 14.9.30.4 GR29: READ "sets the file position indicator to the
      *> value contained in the data item referenced by the RELATIVE
      *> KEY clause" and "If the physical file does not contain such
      *> a record, the invalid key condition exists"; 9.1.13.5 GR3 a):
      *> I-O status 23.  A key read from storage other than RK would
      *> fetch record 2 for key 3 or miss record 2.  Expected: record 3
      *> takes INVALID KEY with status 23, record 2 reads with 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3212I.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT KEYS-FILE ASSIGN TO "pb3212ik.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT REL-FILE ASSIGN TO "pb3212ir.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD KEYS-FILE IS EXTERNAL.
       01 KEYS-REC.
          05 RK PIC 9(4).
          05 FILLER PIC X(6).
       FD REL-FILE IS EXTERNAL.
       01 REL-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS IS EXTERNAL PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT REL-FILE
           MOVE 2 TO RK
           MOVE "SECOND" TO REL-REC
           WRITE REL-REC INVALID KEY DISPLAY "WRITE 2 INVALID".
           CLOSE REL-FILE
           OPEN INPUT REL-FILE
           MOVE 3 TO RK
           READ REL-FILE
               INVALID KEY DISPLAY "READ 3 INVALID " FS
               NOT INVALID KEY DISPLAY "READ 3 FOUND " FS.
           MOVE 2 TO RK
           READ REL-FILE
               INVALID KEY DISPLAY "READ 2 INVALID " FS
               NOT INVALID KEY DISPLAY "READ 2 " REL-REC " " FS.
           CLOSE REL-FILE
           STOP RUN.
