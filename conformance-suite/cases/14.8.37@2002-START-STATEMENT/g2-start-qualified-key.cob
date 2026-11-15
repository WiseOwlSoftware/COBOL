      *>--- conformance-case
      *> id: g2-start-qualified-key
      *> rules: [2002-SR-14.8.37.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.37.2 SR4 - "Data-name-1 or record-key-name-1 may be
      *> qualified." Each KEY data-name below is qualified, and the
      *> qualification is load-bearing: a second item of the same name
      *> holds a different value.
      *>  . indexed: RECORD KEY K OF R; START IF1 KEY IS EQUAL TO
      *>    K OF R, with K OF W = "A" and K OF R = "B";
      *>  . relative: RELATIVE KEY RK OF RKG (§2002:12.3.4.13.2 SR1
      *>    "Data-name-1 may be qualified."), the data item the KEY
      *>    phrase must name (§2002:14.8.37.2 SR5); START RF1 KEY IS
      *>    EQUAL TO RK OF RKG, with RK OF RKG = 2 and RK OF OTH = 3;
      *>  . record-key-name-1: RECORD KEY KN SOURCE IS K1 K2 on IF2,
      *>    the SOURCE-phrase key SR7 requires, named qualified-
      *>    record-key-name (§2002:8.4.1.1.1 Format 6) KN OF IF2, and
      *>    IF3 declares a second record-key-name KN, so unqualified
      *>    KN would be ambiguous. KN is the concatenation K1,K2
      *>    (§2002:12.3.4.12.3 GR2): record "1XA" has key "A1", record
      *>    "2YB" has key "B2"; K1 = "B", K2 = "2" sets up "B2" (GR17
      *>    a), b): the length of record-key-name-1).
      *> EXPECTED, derived from §2002:14.8.37.3 GR17 (indexed) and GR9
      *> a) (relative), I-O status 00 (§2002:9.1.12.1), the following
      *> READ NEXT using the position established (GR16, GR9 a)):
      *>  I S=00 B2 - positioned at key "B", never at "A1";
      *>  K S=00 2YB - positioned at key "B2", never at "1XA";
      *>  R S=00 R002 - positioned at record 2, never at 3;
      *>  DONE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2STR04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "g2str04i.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS K OF R
               FILE STATUS IS FS.
           SELECT IF2 ASSIGN TO "g2str04k.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS KN SOURCE IS K1 K2
               FILE STATUS IS FS.
           SELECT IF3 ASSIGN TO "g2str04x.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS KN SOURCE IS K3
               FILE STATUS IS FS.
           SELECT RF1 ASSIGN TO "g2str04r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK OF RKG
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD IF1.
       01 R.
          05 K PIC X.
          05 D PIC X.
       FD IF2.
       01 R2.
          05 K2 PIC X.
          05 D2 PIC X.
          05 K1 PIC X.
       FD IF3.
       01 R3.
          05 K3 PIC X.
       FD RF1.
       01 RREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 W.
          05 K PIC X.
       01 RKG.
          05 RK PIC 9(4).
       01 OTH.
          05 RK PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IF1.
           MOVE "A1" TO R. WRITE R.
           MOVE "B2" TO R. WRITE R.
           CLOSE IF1.
           OPEN INPUT IF1.
           MOVE "A" TO K OF W.
           MOVE "B" TO K OF R.
           START IF1 KEY IS EQUAL TO K OF R.
           DISPLAY "I S=" FS WITH NO ADVANCING.
           READ IF1 NEXT.
           DISPLAY " " R.
           CLOSE IF1.
           OPEN OUTPUT IF2.
           MOVE "1XA" TO R2. WRITE R2.
           MOVE "2YB" TO R2. WRITE R2.
           CLOSE IF2.
           OPEN INPUT IF2.
           MOVE "B" TO K1. MOVE "2" TO K2.
           START IF2 KEY IS EQUAL TO KN OF IF2.
           DISPLAY "K S=" FS WITH NO ADVANCING.
           READ IF2 NEXT.
           DISPLAY " " R2.
           CLOSE IF2.
           OPEN OUTPUT RF1.
           MOVE 1 TO RK OF RKG. MOVE "R001" TO RREC. WRITE RREC.
           MOVE 2 TO RK OF RKG. MOVE "R002" TO RREC. WRITE RREC.
           MOVE 3 TO RK OF RKG. MOVE "R003" TO RREC. WRITE RREC.
           CLOSE RF1.
           OPEN INPUT RF1.
           MOVE 2 TO RK OF RKG.
           MOVE 3 TO RK OF OTH.
           START RF1 KEY IS EQUAL TO RK OF RKG.
           DISPLAY "R S=" FS WITH NO ADVANCING.
           READ RF1 NEXT.
           DISPLAY " " RREC.
           CLOSE RF1.
           DISPLAY "DONE".
           STOP RUN.
