*>--- conformance-case
*> id: pb3331_locale_key_sequence_fixed_at_open
*> rules: [GR-12.4.5.7.4-1, GR-14.9.27.4-10, GR-12.3.7.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3331 (review w1123b R1-1) — an indexed file's key collating sequence is a FIXED FILE ATTRIBUTE
      *> (ISO §12.4.5.7.4 GR1: "Each collating sequence is a fixed file attribute and the collating sequence is set
      *> upon the successful execution of an OPEN statement that creates the physical file"). The key here is
      *> ordered by ALPHABET CUR IS LOCALE with no locale-name, which §12.3.7.4 GR7 e) defines by "the locale that
      *> is current at the time the collating sequence is used at runtime"; the file is created under the root
      *> locale (the harness pins the user default to the root), so its order is the root's for its existence.
      *> What each line proves:
      *>   C / O1 / W   — the file is created and opened under the root; seven WRITEs succeed.
      *>   R            — after SET LOCALE LC_COLLATE TO ES, every present key is still found ('00', NOT INVALID KEY
      *>                  taken) and an absent one is '23' with INVALID KEY taken (the order did not move).
      *>   S / N / E    — START NOT LESS THAN "n" and READ NEXT walk the ROOT order (n-tilde is a secondary variant
      *>                  of n there: na, n~a, nb, n~b, nz, oa, ob = 1 4 2 5 3 6 7), never the Spanish one
      *>                  (1 2 3 4 5 6 7); the eighth READ NEXT is '10' with AT END taken.
      *>   O2           — re-opened while ES is current, the connector's sequence differs from the file's fixed one:
      *>                  §14.9.27.4 GR10's file attribute conflict, '39' (FILE STATUS present, no declarative:
      *>                  execution continues).
      *>   O3 / M       — back under the user default (the root), the OPEN succeeds and the walk is the root order.
      *> Non-ASCII appears only inside literals (UTF-8 source); every DISPLAY is ASCII.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3331LK.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET CUR IS LOCALE
           LOCALE ES IS "es-ES".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3331lk.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K
               COLLATING SEQUENCE IS CUR
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R.
           05 K            PIC X(2).
       WORKING-STORAGE SECTION.
       01  FS              PIC XX.
       01  I               PIC 9.
       01  J               PIC 9.
       01  LINE-OUT        PIC X(40).
       01  P               PIC 99.
       01  BRANCH          PIC X.
       01  KS.
           05 FILLER       PIC X(2) VALUE "na".
           05 FILLER       PIC X(2) VALUE "nb".
           05 FILLER       PIC X(2) VALUE "nz".
           05 FILLER       PIC X(2) VALUE "ña".
           05 FILLER       PIC X(2) VALUE "ñb".
           05 FILLER       PIC X(2) VALUE "oa".
           05 FILLER       PIC X(2) VALUE "ob".
       01  KT REDEFINES KS.
           05 KE           PIC X(2) OCCURS 7.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F
           CLOSE F
           DISPLAY "C " FS
           OPEN I-O F
           DISPLAY "O1 " FS
           MOVE SPACES TO LINE-OUT
           MOVE 1 TO P
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 7
               MOVE KE(I) TO K
               WRITE R
               STRING FS " " DELIMITED BY SIZE INTO LINE-OUT WITH POINTER P
           END-PERFORM
           DISPLAY "W " LINE-OUT(1:P - 2)
           SET LOCALE LC_COLLATE TO ES
           MOVE SPACES TO LINE-OUT
           MOVE 1 TO P
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 7
               MOVE KE(I) TO K
               READ F INVALID KEY MOVE "I" TO BRANCH
                   NOT INVALID KEY MOVE "V" TO BRANCH
               END-READ
               STRING FS BRANCH " " DELIMITED BY SIZE INTO LINE-OUT WITH POINTER P
           END-PERFORM
           DISPLAY "R " LINE-OUT(1:P - 2)
           MOVE "nc" TO K
           READ F INVALID KEY MOVE "I" TO BRANCH
               NOT INVALID KEY MOVE "V" TO BRANCH
           END-READ
           DISPLAY "R-ABSENT " FS BRANCH
           PERFORM WALK
           DISPLAY "N " LINE-OUT(1:P - 2)
           CLOSE F
           OPEN I-O F
           DISPLAY "O2 " FS
           SET LOCALE LC_COLLATE TO USER-DEFAULT
           OPEN INPUT F
           DISPLAY "O3 " FS
           PERFORM WALK
           DISPLAY "M " LINE-OUT(1:P - 2)
           CLOSE F
           STOP RUN.
       WALK.
           MOVE "n " TO K
           START F KEY IS NOT LESS THAN K
           DISPLAY "S " FS
           MOVE SPACES TO LINE-OUT
           MOVE 1 TO P
           PERFORM 7 TIMES
               READ F NEXT
               PERFORM VARYING J FROM 1 BY 1 UNTIL J > 7 OR KE(J) = K
                   CONTINUE
               END-PERFORM
               STRING J " " DELIMITED BY SIZE INTO LINE-OUT WITH POINTER P
           END-PERFORM
           READ F NEXT AT END MOVE "A" TO BRANCH
               NOT AT END MOVE "N" TO BRANCH
           END-READ
           DISPLAY "E " FS BRANCH.
