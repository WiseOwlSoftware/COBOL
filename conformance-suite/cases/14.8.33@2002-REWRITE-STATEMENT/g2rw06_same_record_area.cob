      *>--- conformance-case
      *> id: g2rw06_same_record_area
      *> rules: [2002-GR-14.8.33.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.33.3 GR5 - "The logical record released by a
      *>   successful execution of the REWRITE statement is no longer
      *>   available in the record area unless file-name-1 or the
      *>   file-name associated with record-name-1 is specified in a
      *>   SAME RECORD AREA clause. The logical record is also
      *>   available as a record of other file-names referenced in the
      *>   same SAME RECORD AREA clause as file-name-1 or the file-name
      *>   associated with record-name-1, as well as the file
      *>   associated with record-name-1." (cite.py: OK)
      *> §2002:12.3.6.3 GR2 - the files of a record-area SAME clause
      *>   "share a memory area for processing the current logical
      *>   record"; the area "is available to the runtime element when
      *>   any file connector referenced by file-name-1, file-name-2,
      *>   ... is open" (cite.py: OK). F1 and K1 stay open across each
      *>   DISPLAY, so the area is available.
      *> Each leg displays the rewriting file's record and the record
      *> of the other file of its clause, after a successful REWRITE:
      *>   S1 sequential F1, REWRITE F1-REC (area set to BBBBB by a
      *>      MOVE): both F1-REC and F2-REC show BBBBB.
      *>   S2 sequential F1, REWRITE F1-REC FROM W-C ("CCCCC"; the
      *>      implicit MOVE fills the area, §2002:14.8.33.3 GR6):
      *>      both show CCCCC.
      *>   K1 indexed K1, REWRITE K1-REC (key 01, data ZZZ): both
      *>      K1-REC and K2-REC show 01ZZZ.
      *> Fails if a REWRITE clears or replaces the record area of a
      *> file named in SAME RECORD AREA, or if the released record is
      *> not visible as the other file's record. Every record is the
      *> same length (5), so no position beyond the released record
      *> is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RW06.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2rw06a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT F2 ASSIGN TO "g2rw06b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT K1 ASSIGN TO "g2rw06c.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS K1-KEY.
           SELECT K2 ASSIGN TO "g2rw06d.dat"
               ORGANIZATION IS SEQUENTIAL.
       I-O-CONTROL.
           SAME RECORD AREA FOR F1 F2
           SAME RECORD AREA FOR K1 K2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  F1-REC PIC X(5).
       FD  F2.
       01  F2-REC PIC X(5).
       FD  K1.
       01  K1-REC.
           05 K1-KEY PIC X(2).
           05 K1-DAT PIC X(3).
       FD  K2.
       01  K2-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01  W-C PIC X(5) VALUE "CCCCC".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F1.
           MOVE "AAAAA" TO F1-REC.
           WRITE F1-REC.
           CLOSE F1.
           OPEN I-O F1.
           READ F1.
           MOVE "BBBBB" TO F1-REC.
           REWRITE F1-REC.
           DISPLAY "S1 [" F1-REC "] [" F2-REC "]".
           CLOSE F1.
           OPEN I-O F1.
           READ F1.
           REWRITE F1-REC FROM W-C.
           DISPLAY "S2 [" F1-REC "] [" F2-REC "]".
           CLOSE F1.
           OPEN OUTPUT K1.
           MOVE "01AAA" TO K1-REC.
           WRITE K1-REC.
           CLOSE K1.
           OPEN I-O K1.
           MOVE "01" TO K1-KEY.
           READ K1 INVALID KEY DISPLAY "K1-READ-INV" END-READ.
           MOVE "ZZZ" TO K1-DAT.
           REWRITE K1-REC INVALID KEY DISPLAY "K1-RW-INV" END-REWRITE.
           DISPLAY "K1 [" K1-REC "] [" K2-REC "]".
           CLOSE K1.
           STOP RUN.
