      *>--- conformance-case
      *> id: g2clo07_with_lock_releases_locks
      *> rules: [2002-GR-14.8.6.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.3 GR9 -- "The file lock and any record locks
      *> associated with the file connector referenced by file-name-1
      *> are released by the execution of the CLOSE statement." Pinned
      *> for the 2002-only CLOSE WITH LOCK (the plain CLOSE is
      *> cases/14.9.6-CLOSE/pb235_close_unit_locks, accepted at 2002).
      *> WITH LOCK forbids re-opening THIS connector (GR3 symbol e); it
      *> does not keep the file or record locks, which GR9 releases.
      *> Record locks: A and B share the file (SHARING WITH ALL OTHER,
      *> LOCK MODE MANUAL). A's READ WITH LOCK holds record 1, so B's
      *> READ is 51 (§2002:9.1.15: "a record is not accessible to
      *> another file connector"; §2002:9.1.12.7 item 1). CLOSE A WITH
      *> LOCK is 00 and B's next READ is 00 RRRRR; A's re-OPEN is 38.
      *> File lock: X opens SHARING WITH NO OTHER, so Y's OPEN is 61
      *> (§2002:9.1.14 item 1; §2002:9.1.12.8 item 1). CLOSE X WITH
      *> LOCK is 00 and Y's OPEN is now 00. After Y closes, X's re-OPEN
      *> is 38 (§2002:14.8.26.3 GR24) -- tried only once Y is closed,
      *> so 61 cannot also apply.
      *> Fails if WITH LOCK keeps either lock (B-FREED=51, Y-FREED=61)
      *> or loses its own effect (A-RELOCKED or X-RELOCKED not 38).
      *> DOC-A.1-103: execution continues after a 38 because the file
      *> has a FILE STATUS clause and EC-I-O checking is not enabled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CL7K.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-SEED ASSIGN TO "g2c7r.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SEED-ST.
           SELECT F-A ASSIGN TO "g2c7r.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-B ASSIGN TO "g2c7r.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL.
           SELECT F-X ASSIGN TO "g2c7x.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS X-ST
               SHARING WITH NO OTHER.
           SELECT F-Y ASSIGN TO "g2c7x.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS Y-ST
               SHARING WITH NO OTHER.
       DATA DIVISION.
       FILE SECTION.
       FD F-SEED.
       01 SEED-REC PIC X(5).
       FD F-A.
       01 A-REC PIC X(5).
       FD F-B.
       01 B-REC PIC X(5).
       FD F-X.
       01 X-REC PIC X(5).
       FD F-Y.
       01 Y-REC PIC X(5).
       WORKING-STORAGE SECTION.
       01 SEED-ST PIC XX.
       01 A-ST PIC XX.
       01 B-ST PIC XX.
       01 X-ST PIC XX.
       01 Y-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-SEED
           MOVE "RRRRR" TO SEED-REC
           WRITE SEED-REC
           CLOSE F-SEED
           OPEN OUTPUT F-X
           MOVE "XXXXX" TO X-REC
           WRITE X-REC
           CLOSE F-X
           OPEN INPUT F-A
           OPEN INPUT F-B
           READ F-A WITH LOCK AT END CONTINUE END-READ
           DISPLAY "A-LOCKS=" A-ST
           READ F-B AT END CONTINUE END-READ
           DISPLAY "B-BLOCKED=" B-ST
           CLOSE F-A WITH LOCK
           DISPLAY "A-CLOSE-LOCK=" A-ST
           READ F-B AT END CONTINUE END-READ
           DISPLAY "B-FREED=" B-ST " " B-REC
           CLOSE F-B
           OPEN INPUT F-A
           DISPLAY "A-RELOCKED=" A-ST
           OPEN INPUT F-X
           DISPLAY "X-OPEN=" X-ST
           OPEN INPUT F-Y
           DISPLAY "Y-BLOCKED=" Y-ST
           CLOSE F-X WITH LOCK
           DISPLAY "X-CLOSE-LOCK=" X-ST
           OPEN INPUT F-Y
           DISPLAY "Y-FREED=" Y-ST
           CLOSE F-Y
           DISPLAY "Y-CLOSE=" Y-ST
           OPEN INPUT F-X
           DISPLAY "X-RELOCKED=" X-ST
           STOP RUN.
