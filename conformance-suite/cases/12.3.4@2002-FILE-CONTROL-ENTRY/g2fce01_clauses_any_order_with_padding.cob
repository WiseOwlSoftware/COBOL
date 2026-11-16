      *>--- conformance-case
      *> id: g2fce01_clauses_any_order_with_padding
      *> rules: [2002-SR-12.3.4.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.2 SR1 - SELECT first, the clauses after it in any
      *> order, including the 2002 Format 3 PADDING CHARACTER clause.
      *>   cite.py --check "§2002:12.3.4.2 SR1" "The clauses that follow
      *>   the SELECT clause may appear in any order" -> OK
      *> The 2002 Format 3 (sequential) prints PADDING CHARACTER among the
      *> clauses that follow SELECT, so SR1 frees its position too:
      *>   F1 - PADDING CHARACTER IS "*" straight after SELECT, then
      *>        RESERVE, ORGANIZATION, FILE STATUS, ASSIGN last;
      *>   F2 - FILE STATUS, ACCESS MODE, ASSIGN, then PADDING CHARACTER
      *>        IS PADC last (data-name form);
      *>   FX - Format 1: RECORD KEY, FILE STATUS, ACCESS MODE,
      *>        ORGANIZATION, ASSIGN last;
      *>   FR - Format 2: RELATIVE KEY, ORGANIZATION, ACCESS MODE, FILE
      *>        STATUS, ASSIGN last.
      *> The order in which a clause is written carries no meaning, so
      *> each file must behave as if its clauses were in format order.
      *> The SELECT-first half is the refusal g2fce02-select-not-first.
      *> §2002:12.3.4.10: "The PADDING CHARACTER clause is an obsolete
      *> element in this International Standard" (cite.py OK): the
      *> compiler may warn (COBOL0903); that does not fail the case.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   F1, F2 - sequential: §2002:12.3.4.4.3 GR2a, records are read in
      *>     the order WRITE established (cite.py OK); no record consists
      *>     solely of padding characters, so none is ignored
      *>     (§2002:12.3.4.10.3 GR1, cite.py OK); after the last record
      *>     the READ takes AT END => F1=ABCD / F1=EFGH / F1=END and
      *>     F2=WXYZ / F2=END; every OPEN reports 00 (the FS-O lines).
      *>   FX - indexed, random: READ with RECORD KEY K2 returns the
      *>     record written with key K2 => FX=K2TWO.
      *>   FR - relative, random: READ with RELATIVE KEY 3 returns the
      *>     record written at relative record number 3 => FR=THREE.
      *> A leg fails if any ordering is refused at 2002, or if a clause
      *> written out of format order is lost (wrong status data item,
      *> wrong key, wrong organization).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FCE01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 PADDING CHARACTER IS "*"
               RESERVE 2 AREAS
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1
               ASSIGN TO "G2FCE1.DAT".
           SELECT F2 FILE STATUS IS ST2
               ACCESS MODE IS SEQUENTIAL
               ASSIGN TO "G2FCE2.DAT"
               PADDING CHARACTER IS PADC.
           SELECT FX RECORD KEY IS FX-KEY
               FILE STATUS IS STX
               ACCESS MODE IS RANDOM
               ORGANIZATION IS INDEXED
               ASSIGN TO "G2FCEX.DAT".
           SELECT FR RELATIVE KEY IS RK
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               FILE STATUS IS STR
               ASSIGN TO "G2FCER.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1          PIC X(4).
       FD  F2.
       01  R2          PIC X(4).
       FD  FX.
       01  RX.
           05  FX-KEY  PIC XX.
           05  FX-DAT  PIC X(3).
       FD  FR.
       01  RR          PIC X(5).
       WORKING-STORAGE SECTION.
       01  PADC        PIC X VALUE "#".
       01  ST1         PIC XX.
       01  ST2         PIC XX.
       01  STX         PIC XX.
       01  STR         PIC XX.
       01  RK          PIC 9.
       01  WS-EOF      PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT F1.
           DISPLAY "FS-O1=" ST1.
           MOVE "ABCD" TO R1. WRITE R1.
           MOVE "EFGH" TO R1. WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ F1
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "F1=END"
                   NOT AT END DISPLAY "F1=" R1
               END-READ
           END-PERFORM.
           CLOSE F1.
           OPEN OUTPUT F2.
           DISPLAY "FS-O2=" ST2.
           MOVE "WXYZ" TO R2. WRITE R2.
           CLOSE F2.
           OPEN INPUT F2.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ F2
                   AT END MOVE "Y" TO WS-EOF
                          DISPLAY "F2=END"
                   NOT AT END DISPLAY "F2=" R2
               END-READ
           END-PERFORM.
           CLOSE F2.
           OPEN OUTPUT FX.
           DISPLAY "FS-OX=" STX.
           MOVE "K1ONE" TO RX. WRITE RX INVALID KEY CONTINUE END-WRITE.
           MOVE "K2TWO" TO RX. WRITE RX INVALID KEY CONTINUE END-WRITE.
           CLOSE FX.
           OPEN INPUT FX.
           MOVE "K2" TO FX-KEY.
           MOVE SPACES TO FX-DAT.
           READ FX INVALID KEY DISPLAY "FX=MISS" END-READ.
           DISPLAY "FX=" RX.
           CLOSE FX.
           OPEN OUTPUT FR.
           DISPLAY "FS-OR=" STR.
           MOVE 1 TO RK. MOVE "ONE" TO RR.
           WRITE RR INVALID KEY CONTINUE END-WRITE.
           MOVE 3 TO RK. MOVE "THREE" TO RR.
           WRITE RR INVALID KEY CONTINUE END-WRITE.
           CLOSE FR.
           OPEN INPUT FR.
           MOVE 3 TO RK.
           MOVE SPACES TO RR.
           READ FR INVALID KEY DISPLAY "FR=MISS" END-READ.
           DISPLAY "FR=" RR.
           CLOSE FR.
           STOP RUN.
