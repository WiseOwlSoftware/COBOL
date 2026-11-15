      *>--- conformance-case
      *> id: g2002_open_not_open_connector
      *> rules: [2002-GR-14.8.26.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.3 GR4 - "When a file connector is not open, no
      *> statement shall be executed that references the associated
      *> file-name, either explicitly or implicitly, except for a MERGE or
      *> SORT statement with the USING or GIVING phrase, or an OPEN
      *> statement." Both halves are exercised on connectors that are not
      *> open: the forbidden statements each answer their own 2002 logic
      *> error status and leave the file alone, and the two exempt verbs
      *> (SORT USING/GIVING and OPEN) work.
      *> The run is deterministic because the forbidden statements are
      *> caught by a format 1 USE declarative that ends in RESUME AT NEXT
      *> STATEMENT (§2002:14.8.31.3 GR2), so no implementor choice about
      *> continuing after a fatal I-O status (§2002:9.1.12) is reached;
      *> USE counts its runs.
      *>   OPEN=00, W1, W2=00, CLOSE=00: F is created with BBBB, AAAA.
      *>   F is now not open:
      *>   CLOSE-NOT-OPEN=42  §2002:9.1.12.6 item 2, "A CLOSE or UNLOCK
      *>              statement is attempted for a file connector that is not in an
      *>              open mode"; USE=1.
      *>   UNLOCK=42  the same item; USE=2.
      *>   READ=47    item 7, "a READ or START statement is attempted
      *>              referencing a file connector that is not open in the
      *>              input or I-O mode"; the AT END imperative does not
      *>              run ('47' is no at end condition); USE=3.
      *>   START=47   the same item 7; USE=4.
      *>   WRITE=48   item 8 a), sequential access, "not open in the extend
      *>              or output mode"; USE=5.
      *>   REWRITE=4  item 9 ('49') and item 3 ('43': the last statement
      *>              was not a successful READ) both apply, and "If more
      *>              than one value applies, the implementor determines
      *>              which" (§2002:9.1.12), so only the logic-error class
      *>              digit '4' is pinned; USE=6.
      *>   DELETE=49  R is RANDOM access, so item 3 (sequential access
      *>              only) cannot apply: item 9 alone; USE=7.
      *>   The exempt SORT: F (USING) and G (GIVING) are both not open,
      *>   which §2002:14.8.36.3 GR8 a) and c) require; G receives F's
      *>   records in ascending key order: G1=AAAA, G2=BBBB, G3=10.
      *>   The exempt OPEN: OPEN INPUT F succeeds (REOPEN=00), and F still
      *>   holds exactly BBBB, AAAA, then '10' (§2002:9.1.12.3 item 1 a):
      *>   the forbidden WRITE added nothing (§2002:14.8.47.3 GR13, "the
      *>   write operation does not take place"). USE stays 7: the AT END
      *>   phrase takes precedence (§2002:9.1.12).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g2opn04f.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS ST.
           SELECT R ASSIGN TO "g2opn04r.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK
               FILE STATUS IS RS.
           SELECT G ASSIGN TO "g2opn04g.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS GS.
           SELECT W ASSIGN TO "g2opn04w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       FD R.
       01 R-REC PIC X(4).
       FD G.
       01 G-REC PIC X(4).
       SD W.
       01 W-REC.
          05 W-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       01 RS PIC XX.
       01 GS PIC XX.
       01 RK PIC 9(4) VALUE 1.
       01 WS-USE PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IO-ERR SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F R.
       IO-ERR-P.
           ADD 1 TO WS-USE
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT F
           DISPLAY "OPEN=" ST
           MOVE "BBBB" TO F-REC
           WRITE F-REC
           DISPLAY "W1=" ST
           MOVE "AAAA" TO F-REC
           WRITE F-REC
           DISPLAY "W2=" ST
           CLOSE F
           DISPLAY "CLOSE=" ST
           CLOSE F
           DISPLAY "CLOSE-NOT-OPEN=" ST " USE=" WS-USE
           UNLOCK F
           DISPLAY "UNLOCK=" ST " USE=" WS-USE
           READ F AT END DISPLAY "READ AT END RAN" END-READ
           DISPLAY "READ=" ST " USE=" WS-USE
           START F FIRST
           DISPLAY "START=" ST " USE=" WS-USE
           WRITE F-REC
           DISPLAY "WRITE=" ST " USE=" WS-USE
           REWRITE F-REC
           DISPLAY "REWRITE=" ST(1:1) " USE=" WS-USE
           DELETE R RECORD INVALID KEY DISPLAY "DELETE INVALID KEY"
           END-DELETE
           DISPLAY "DELETE=" RS " USE=" WS-USE
           SORT W ON ASCENDING KEY W-KEY USING F GIVING G
           OPEN INPUT G
           READ G AT END MOVE "EOF" TO G-REC END-READ
           DISPLAY "G1=" GS " " G-REC
           READ G AT END MOVE "EOF" TO G-REC END-READ
           DISPLAY "G2=" GS " " G-REC
           READ G AT END MOVE "EOF" TO G-REC END-READ
           DISPLAY "G3=" GS
           CLOSE G
           OPEN INPUT F
           DISPLAY "REOPEN=" ST " USE=" WS-USE
           READ F AT END MOVE "EOF" TO F-REC END-READ
           DISPLAY "F1=" ST " " F-REC
           READ F AT END MOVE "EOF" TO F-REC END-READ
           DISPLAY "F2=" ST " " F-REC
           READ F AT END MOVE "EOF" TO F-REC END-READ
           DISPLAY "F3=" ST " USE=" WS-USE
           CLOSE F
           STOP RUN.
