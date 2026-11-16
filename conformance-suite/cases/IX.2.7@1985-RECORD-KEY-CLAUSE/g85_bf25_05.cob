      *>--- conformance-case
      *> id: g85_bf25_05
      *> rules: [1985-GR-IX.2.7.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.7.4 GR2 - the prime record key presented when an
      *> indexed file is opened is the one it was created with.
      *  GR2: "The data description of data-name-1 as well as its
      *  relative location within a record must be the same as that
      *  used when the file was created." §1985:IX.4.4.4 GR8: a file
      *  attribute conflict makes the OPEN unsuccessful, and "The
      *  implementor defines which of the fixed file attributes are
      *  validated"; WiseOwl COBOL validates each key's position and
      *  length (docs/conformance/7/DOC-A.1-129.md), so a different
      *  prime key is status 39 (§1985:IX.1.3.4), and §1985:IX.4.4.4
      *  GR19 leaves the physical file unaffected.
      *  The file is created through IX-MAKE: key X(4) at character
      *  positions 3-6 of a 20-character record.
      *  S1  Create and write K001, K002, K003 in key order: 00 each.
      *  S2  IX-SAME names other data-names but describes the key the
      *      same way at the same place: OPEN 00, and a random READ of
      *      key K002 finds the record written with it.
      *  S3  IX-MOVED has the key X(4) at positions 1-4: 39.
      *  S4  IX-LONG has the key X(6) at positions 3-8: 39.
      *  S5  After the failed OPENs IX-MAKE reads K003 at random: 00.
      *  A leg fails if a matching key description is refused, a
      *  READ by key misses, or a moved or resized key is accepted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2505.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-MAKE ASSIGN TO "g85bf2505.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS MK-KEY
               FILE STATUS IS MK-ST.
           SELECT IX-SAME ASSIGN TO "g85bf2505.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS SM-KEY
               FILE STATUS IS SM-ST.
           SELECT IX-MOVED ASSIGN TO "g85bf2505.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS MV-KEY
               FILE STATUS IS MV-ST.
           SELECT IX-LONG ASSIGN TO "g85bf2505.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS LG-KEY
               FILE STATUS IS LG-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-MAKE.
       01  MK-REC.
           05 MK-PFX  PIC X(2).
           05 MK-KEY  PIC X(4).
           05 MK-DATA PIC X(14).
       FD  IX-SAME.
       01  SM-REC.
           05 SM-A    PIC X.
           05 SM-B    PIC X.
           05 SM-KEY  PIC X(4).
           05 SM-REST PIC X(14).
       FD  IX-MOVED.
       01  MV-REC.
           05 MV-KEY  PIC X(4).
           05 MV-REST PIC X(16).
       FD  IX-LONG.
       01  LG-REC.
           05 LG-PFX  PIC X(2).
           05 LG-KEY  PIC X(6).
           05 LG-REST PIC X(12).
       WORKING-STORAGE SECTION.
       01  MK-ST PIC XX.
       01  SM-ST PIC XX.
       01  MV-ST PIC XX.
       01  LG-ST PIC XX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT IX-MAKE.
           MOVE "AA" TO MK-PFX.
           MOVE "K001" TO MK-KEY.
           MOVE "ONE" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W1-INVALID".
           DISPLAY "S1-W1=" MK-ST.
           MOVE "BB" TO MK-PFX.
           MOVE "K002" TO MK-KEY.
           MOVE "TWO" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W2-INVALID".
           DISPLAY "S1-W2=" MK-ST.
           MOVE "CC" TO MK-PFX.
           MOVE "K003" TO MK-KEY.
           MOVE "THREE" TO MK-DATA.
           WRITE MK-REC INVALID KEY DISPLAY "S1-W3-INVALID".
           DISPLAY "S1-W3=" MK-ST.
           CLOSE IX-MAKE.
           OPEN INPUT IX-SAME.
           DISPLAY "S2-OPEN=" SM-ST.
           MOVE "K002" TO SM-KEY.
           READ IX-SAME INVALID KEY DISPLAY "S2-INVALID".
           DISPLAY "S2-READ=" SM-ST " [" SM-REC "]".
           CLOSE IX-SAME.
           OPEN INPUT IX-MOVED.
           DISPLAY "S3-MOVED=" MV-ST.
           IF MV-ST = "00"
               CLOSE IX-MOVED.
           OPEN INPUT IX-LONG.
           DISPLAY "S4-LONG=" LG-ST.
           IF LG-ST = "00"
               CLOSE IX-LONG.
           OPEN INPUT IX-MAKE.
           DISPLAY "S5-OPEN=" MK-ST.
           MOVE "K003" TO MK-KEY.
           READ IX-MAKE INVALID KEY DISPLAY "S5-INVALID".
           DISPLAY "S5-READ=" MK-ST " [" MK-REC "]".
           CLOSE IX-MAKE.
           STOP RUN.
