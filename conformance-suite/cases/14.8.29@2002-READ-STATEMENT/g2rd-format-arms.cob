      *>--- conformance-case
      *> id: g2rd-format-arms
      *> rules: [2002-FMT-14.8.29.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.29.1 (general format) - the READ arms the other
      *>   2002 READ cases do not write: format 1 PREVIOUS, INTO, AT END
      *>   with NOT AT END, ADVANCING ON LOCK (ON written), WITH LOCK,
      *>   NEXT and RECORD written and omitted, a retry-phrase (S3),
      *>   AT END ended by a period; format 2 INTO, WITH LOCK, WITH
      *>   NO LOCK, INVALID KEY
      *>   with NOT INVALID KEY, KEY IS data-name-1 and KEY IS
      *>   record-key-name-1 (AK, an ALTERNATE RECORD KEY ... SOURCE
      *>   IS IX-D). (NEXT with the lock brackets, format 2 RETRY and
      *>   ADVANCING LOCK without ON: pb331_read_lock_brackets.)
      *> DERIVATION (2002 text only). IX holds 1one 2two 3thr; LOCK MODE
      *>   IS MANUAL, so the LOCK phrases are allowed (SR5) and no other
      *>   connector holds a lock (GR7, GR11, GR19 change nothing here).
      *>   R1 random READ of key 2 INTO W: NOT INVALID KEY runs after
      *>     the move (GR12c, GR4b): R1 2two.
      *>   R2 key 9 is absent: the invalid key condition, status 23
      *>     (GR29, §2002:9.1.12.4): R2 INVALID 23.
      *>   R3 KEY IS AK with IX-D = thr and IX-K = 9: AK is the key of
      *>     reference (GR27), so the prime key's 9 is not consulted:
      *>     R3 3thr.
      *>   START KEY = IX-K with 3 sets the position to 3
      *>     (§2002:14.8.37.3 GR17e). P1 PREVIOUS takes the first
      *>     record <= 3 (GR18c): P1 3thr. P2 takes the record < 3
      *>     (GR18d): P2 2two. P3: P3 1one. P4 finds none: at end, status 10 (GR18,
      *>     GR21b): P4 END 10.
      *>   SQ holds AAAA BBBB: S1 NEXT INTO: S1 AAAA; S2 (NEXT implied,
      *>     SR7): S2 BBBB; S3 RETRY 2 TIMES meets no sharing or record
      *>     operation conflict on this one connector, so RETRY has no
      *>     effect (§2002:14.6.8.2 GR4), and at end: S3 END 10.
      *> A compiler that refuses a form, reads forward on PREVIOUS,
      *>   ignores the alternate key or binds AT END and NOT AT END
      *>   (or the INVALID KEY pair) the wrong way round fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RDFMT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX ASSIGN TO "g2rdfmtx.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-K
               ALTERNATE RECORD KEY IS AK SOURCE IS IX-D
               LOCK MODE IS MANUAL
               FILE STATUS IS FS.
           SELECT SQ ASSIGN TO "g2rdfmts.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD IX.
       01 IX-R.
          05 IX-K PIC X.
          05 IX-D PIC X(3).
       FD SQ.
       01 SQ-R PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS  PIC XX.
       01 FS2 PIC XX.
       01 W   PIC X(4).
       PROCEDURE DIVISION.
       M-0.
           OPEN OUTPUT IX SQ
           MOVE "1one" TO IX-R WRITE IX-R
           MOVE "2two" TO IX-R WRITE IX-R
           MOVE "3thr" TO IX-R WRITE IX-R
           WRITE SQ-R FROM "AAAA"
           WRITE SQ-R FROM "BBBB"
           CLOSE IX SQ
           OPEN I-O IX
           MOVE "2" TO IX-K
           READ IX RECORD INTO W WITH LOCK KEY IS IX-K
               INVALID KEY DISPLAY "R1 INVALID"
               NOT INVALID KEY DISPLAY "R1 " W
           END-READ
           MOVE "9" TO IX-K
           READ IX WITH NO LOCK KEY IS IX-K
               INVALID KEY DISPLAY "R2 INVALID " FS
               NOT INVALID KEY DISPLAY "R2 FOUND"
           END-READ
           MOVE "thr" TO IX-D
           MOVE "9" TO IX-K
           READ IX KEY IS AK
               INVALID KEY DISPLAY "R3 INVALID"
               NOT INVALID KEY DISPLAY "R3 " IX-R
           END-READ
           MOVE "3" TO IX-K
           START IX KEY = IX-K
           READ IX PREVIOUS RECORD INTO W WITH LOCK
               AT END DISPLAY "P1 END"
               NOT AT END DISPLAY "P1 " W
           END-READ
           READ IX PREVIOUS
               AT END DISPLAY "P2 END"
               NOT AT END DISPLAY "P2 " IX-R
           END-READ
           READ IX PREVIOUS RECORD IGNORING LOCK
           DISPLAY "P3 " IX-R
           READ IX PREVIOUS
               AT END DISPLAY "P4 END " FS
               NOT AT END DISPLAY "P4 " IX-R
           END-READ
           CLOSE IX
           OPEN INPUT SQ
           READ SQ NEXT RECORD INTO W ADVANCING ON LOCK
               AT END DISPLAY "S1 END"
               NOT AT END DISPLAY "S1 " W
           END-READ
           READ SQ RECORD
               AT END DISPLAY "S2 END"
               NOT AT END DISPLAY "S2 " SQ-R.
           READ SQ RETRY 2 TIMES AT END DISPLAY "S3 END " FS2.
           CLOSE SQ
           STOP RUN.
