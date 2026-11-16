      *>--- conformance-case
      *> id: g85_bf25_07
      *> rules: [1985-GR-VII.4.4.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.4.4 GR8 - READ ... INTO on a sequential file is
      *> the READ followed by a MOVE of the current record.
      *  GR8 a: the same READ without INTO; b: the current record is
      *  moved "according to the rules for the MOVE statement", its
      *  size by the RECORD clause; with RECORD IS VARYING "the
      *  implied move is a group move"; "The implied MOVE statement
      *  does not occur if the execution of the READ statement was
      *  unsuccessful"; subscripting of identifier-1 "is evaluated
      *  after the record has been read"; the record is available in
      *  both areas.
      *  A  F-X: one record PIC X(5) "ABCDE", an elementary MOVE to
      *     W-J8 PIC X(8) JUSTIFIED RIGHT (§1985:VI.5.6.4 GR1 aligns
      *     at the right, space fill on the left): "   ABCDE"; the
      *     record area X-REC still holds "ABCDE".
      *  B  F-V: RECORD IS VARYING 1 TO 10 DEPENDING ON V-LEN, record
      *     "ABC" written with V-LEN 3. The READ sets V-LEN to 3
      *     (§1985:VII.3.8.4 GR11), GR12 a makes those 3 characters
      *     the sender, and the group move ignores the receiver's
      *     editing (§1985:VI.6.19.4 GR5: "filled without consideration
      *     for the individual elementary or group items"): W-ED PIC
      *     XXBXX gets "ABC  ", where an edited move would give
      *     "AB C ".
      *  C  F-S: records "A3" and "B1" read INTO W-E (S-SEQ), S-SEQ
      *     being in the record and set to 2 before the first READ:
      *     the subscript is the value just read, not that 2,
      *     so W-E (3) = "A3" and W-E (1) = "B1"; W-E (2) keeps "..".
      *  D  The next READ of F-S is at end (status 10): no MOVE, so
      *     W-KEEP keeps "KEEP".
      *  A leg fails if the move is skipped, uses the wrong move
      *  kind or size, subscripts before the read, or moves on an
      *  unsuccessful READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2507.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-X ASSIGN TO "g85bf2507x.dat"
               FILE STATUS IS X-ST.
           SELECT F-V ASSIGN TO "g85bf2507v.dat"
               FILE STATUS IS V-ST.
           SELECT F-S ASSIGN TO "g85bf2507s.dat"
               FILE STATUS IS S-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F-X.
       01  X-REC PIC X(5).
       FD  F-V
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON V-LEN.
       01  V-REC PIC X(10).
       FD  F-S.
       01  S-REC.
           05 S-K   PIC X.
           05 S-SEQ PIC 9.
       WORKING-STORAGE SECTION.
       01  X-ST   PIC XX.
       01  V-ST   PIC XX.
       01  S-ST   PIC XX.
       01  V-LEN  PIC 99.
       01  W-J8   PIC X(8) JUSTIFIED RIGHT.
       01  W-ED   PIC XXBXX.
       01  W-T.
           05 W-E PIC XX OCCURS 3 TIMES.
       01  W-KEEP PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F-X.
           MOVE "ABCDE" TO X-REC.
           WRITE X-REC.
           CLOSE F-X.
           MOVE ALL "." TO W-J8.
           OPEN INPUT F-X.
           READ F-X INTO W-J8 AT END DISPLAY "A-END".
           DISPLAY "A=" X-ST " [" W-J8 "] [" X-REC "]".
           CLOSE F-X.
           OPEN OUTPUT F-V.
           MOVE 3 TO V-LEN.
           MOVE "ABC" TO V-REC.
           WRITE V-REC.
           CLOSE F-V.
           MOVE 0 TO V-LEN.
           MOVE ALL "." TO W-ED.
           OPEN INPUT F-V.
           READ F-V INTO W-ED AT END DISPLAY "B-END".
           DISPLAY "B=" V-ST " LEN=" V-LEN " [" W-ED "]".
           CLOSE F-V.
           OPEN OUTPUT F-S.
           MOVE "A3" TO S-REC.
           WRITE S-REC.
           MOVE "B1" TO S-REC.
           WRITE S-REC.
           CLOSE F-S.
           MOVE ALL "." TO W-T.
           MOVE "KEEP" TO W-KEEP.
           OPEN INPUT F-S.
           MOVE 2 TO S-SEQ.
           READ F-S INTO W-E (S-SEQ) AT END DISPLAY "C1-END".
           READ F-S INTO W-E (S-SEQ) AT END DISPLAY "C2-END".
           DISPLAY "C=" S-ST " [" W-T "]".
           READ F-S INTO W-KEEP AT END DISPLAY "D-END=" S-ST.
           DISPLAY "D=[" W-KEEP "]".
           CLOSE F-S.
           STOP RUN.
