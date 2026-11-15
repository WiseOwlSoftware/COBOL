      *>--- conformance-case
      *> id: g2_read_self_lock_ignored
      *> rules: [2002-GR-14.8.29.3-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.29.3 GR7 - "If record locking is enabled for the
      *> file connector referenced by file-name-1 and the record
      *> identified for access by the general rules for the READ
      *> statement is locked by that file connector, the record lock is
      *> ignored and the READ operation proceeds as if the record were
      *> not locked." §2002:9.1.15: "A locked record may be re-accessed
      *> by the same file connector that holds the lock."
      *> Locking: §2002:12.3.4.8.3 GR4 AUTOMATIC - "Records are locked
      *> when any READ statement is executed"; GR7 MULTIPLE - "a file
      *> connector is permitted to have more than one record of a file
      *> locked". MULTIPLE is load-bearing: under single locking GR6
      *> ("Execution of any I-O statement except START releases any
      *> previously locked record") a re-read could not tell "ignored"
      *> from "released first". §2002:12.3.4.8.2 SR1 bars MULTIPLE on
      *> sequential organization or access, hence relative + RANDOM.
      *> R1 F1 reads record 1 and locks it: 00 R001.
      *> R2 F2 reads record 1: locked by another connector, so the
      *>    record operation conflict condition (§2002:14.8.29.3 GR8)
      *>    with no RETRY phrase: 51 (§2002:9.1.12.7). Proves the lock.
      *> R3 F1 reads record 2: 00 R002; record 1 stays locked.
      *> R4 F1 re-reads record 1: GR7, its own lock is ignored: 00
      *>    R001 (a 51 here would treat its own lock as foreign).
      *> R5 F2 is still refused 51: R4 ignored the lock, it did not
      *>    release it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RD07.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FS ASSIGN TO "g2rd07.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST0.
           SELECT F1 ASSIGN TO "g2rd07.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K1
               FILE STATUS IS ST1
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS.
           SELECT F2 ASSIGN TO "g2rd07.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS K2
               FILE STATUS IS ST2
               SHARING WITH ALL OTHER
               LOCK MODE IS AUTOMATIC WITH LOCK ON MULTIPLE RECORDS.
       DATA DIVISION.
       FILE SECTION.
       FD FS.
       01 S-REC PIC X(4).
       FD F1.
       01 R1-REC PIC X(4).
       FD F2.
       01 R2-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST0 PIC XX.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       01 K1  PIC 9(4).
       01 K2  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT FS.
           MOVE "R001" TO S-REC.
           WRITE S-REC.
           MOVE "R002" TO S-REC.
           WRITE S-REC.
           CLOSE FS.
           OPEN INPUT F1.
           OPEN INPUT F2.
           MOVE 1 TO K1.
           READ F1.
           DISPLAY "R1=" ST1 " " R1-REC.
           MOVE 1 TO K2.
           READ F2.
           DISPLAY "R2=" ST2.
           MOVE 2 TO K1.
           READ F1.
           DISPLAY "R3=" ST1 " " R1-REC.
           MOVE 1 TO K1.
           READ F1.
           DISPLAY "R4=" ST1 " " R1-REC.
           MOVE 1 TO K2.
           READ F2.
           DISPLAY "R5=" ST2.
           CLOSE F1.
           CLOSE F2.
           STOP RUN.
