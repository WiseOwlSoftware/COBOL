      *>--- conformance-case
      *> id: g85_bf25_08
      *> rules: [1985-GR-VIII.4.5.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.5.4 GR8 - READ ... INTO on a relative file is the
      *> READ followed by a MOVE of the current record.
      *  GR8 a: the same READ without INTO; b: the current record is
      *  moved "according to the rules for the MOVE statement", its
      *  size by the RECORD clause; with RECORD IS VARYING "the
      *  implied move is a group move"; "The implied MOVE statement
      *  does not occur if the execution of the READ statement was
      *  unsuccessful"; subscripting of identifier-1 "is evaluated
      *  after the record has been read"; the record is available in
      *  both areas.
      *  A  R-X (random): record 2 is "ABCDE" (one 01 PIC X(5)); a
      *     format 2 READ INTO W-J8 PIC X(8) JUSTIFIED RIGHT is an
      *     elementary move (§1985:VI.5.6.4 GR1): "   ABCDE"; the
      *     record area still holds "ABCDE".
      *  B  R-X: record 7 does not exist: INVALID KEY, status 23
      *     (§1985:VIII.1.3.4), and no MOVE: W-KEEP keeps "KEEP".
      *  C  R-V (sequential): RECORD IS VARYING 1 TO 10 DEPENDING ON
      *     V-LEN, record "ABC" written with V-LEN 3. The READ sets
      *     V-LEN to 3 (§1985:VII.3.8.4 GR11, the RECORD clause the
      *     Relative I-O module adopts by §1985:VIII.3.2.4 GR5), those 3
      *     characters are
      *     the sender, and the group move ignores the receiver's
      *     editing (§1985:VI.6.19.4 GR5): W-ED PIC XXBXX gets "ABC  ".
      *  D  R-S (sequential): records "A3" and "B1" read INTO
      *     W-E (S-SEQ), S-SEQ being in the record and set to 2 before
      *     the first READ: W-E (3) = "A3", W-E (1) = "B1", W-E (2)
      *     keeps "..". The next READ is at end (10): W-KEEP is kept.
      *  A leg fails if the move is skipped, uses the wrong move
      *  kind or size, subscripts before the read, or moves on an
      *  unsuccessful READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2508.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT R-X ASSIGN TO "g85bf2508x.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS X-RK
               FILE STATUS IS X-ST.
           SELECT R-V ASSIGN TO "g85bf2508v.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS V-ST.
           SELECT R-S ASSIGN TO "g85bf2508s.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS S-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  R-X.
       01  X-REC PIC X(5).
       FD  R-V
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON V-LEN.
       01  V-REC PIC X(10).
       FD  R-S.
       01  S-REC.
           05 S-K   PIC X.
           05 S-SEQ PIC 9.
       WORKING-STORAGE SECTION.
       01  X-ST   PIC XX.
       01  V-ST   PIC XX.
       01  S-ST   PIC XX.
       01  X-RK   PIC 99.
       01  V-LEN  PIC 99.
       01  W-J8   PIC X(8) JUSTIFIED RIGHT.
       01  W-ED   PIC XXBXX.
       01  W-T.
           05 W-E PIC XX OCCURS 3 TIMES.
       01  W-KEEP PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT R-X.
           MOVE 2 TO X-RK.
           MOVE "ABCDE" TO X-REC.
           WRITE X-REC INVALID KEY DISPLAY "A-W-INVALID".
           CLOSE R-X.
           MOVE ALL "." TO W-J8.
           MOVE "KEEP" TO W-KEEP.
           OPEN INPUT R-X.
           MOVE 2 TO X-RK.
           READ R-X INTO W-J8 INVALID KEY DISPLAY "A-INVALID".
           DISPLAY "A=" X-ST " [" W-J8 "] [" X-REC "]".
           MOVE 7 TO X-RK.
           READ R-X INTO W-KEEP INVALID KEY DISPLAY "B-INVALID".
           DISPLAY "B=" X-ST " [" W-KEEP "]".
           CLOSE R-X.
           OPEN OUTPUT R-V.
           MOVE 3 TO V-LEN.
           MOVE "ABC" TO V-REC.
           WRITE V-REC INVALID KEY DISPLAY "C-W-INVALID".
           CLOSE R-V.
           MOVE 0 TO V-LEN.
           MOVE ALL "." TO W-ED.
           OPEN INPUT R-V.
           READ R-V INTO W-ED AT END DISPLAY "C-END".
           DISPLAY "C=" V-ST " LEN=" V-LEN " [" W-ED "]".
           CLOSE R-V.
           OPEN OUTPUT R-S.
           MOVE "A3" TO S-REC.
           WRITE S-REC INVALID KEY DISPLAY "D-W1-INVALID".
           MOVE "B1" TO S-REC.
           WRITE S-REC INVALID KEY DISPLAY "D-W2-INVALID".
           CLOSE R-S.
           MOVE ALL "." TO W-T.
           OPEN INPUT R-S.
           MOVE 2 TO S-SEQ.
           READ R-S INTO W-E (S-SEQ) AT END DISPLAY "D1-END".
           READ R-S INTO W-E (S-SEQ) AT END DISPLAY "D2-END".
           DISPLAY "D=" S-ST " [" W-T "]".
           READ R-S INTO W-KEEP AT END DISPLAY "D-END=" S-ST.
           DISPLAY "D=[" W-KEEP "]".
           CLOSE R-S.
           STOP RUN.
