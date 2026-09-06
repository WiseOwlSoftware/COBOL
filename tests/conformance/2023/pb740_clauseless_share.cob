       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB740SHR.
      *> kb/Work PB740 — the operating environment's share mode is ISO
      *> 9.1.15's FILE LOCK, and it may not add a second gate inside the run
      *> unit. 9.1.15 names both audiences: "Multiple paths of access may
      *> exist in the same runtime element, contained elements, separate
      *> runtime elements within the same run unit, or runtime elements in
      *> different run units", and for the paths inside this run unit the
      *> gate is stated exactly — "Before access to a shared physical file is
      *> allowed through an OPEN statement, the sharing mode and the open
      *> mode of that OPEN statement shall be allowed by all other file
      *> connectors that are currently associated with the physical file, as
      *> described in 9.1.13, I-O status; 14.9.27, OPEN statement; and Table
      *> 19". For the other run units it is the file lock: "The successful
      *> opening of a file establishes a file lock for the applicable sharing
      *> rules, thereby preventing other run units from opening that file
      *> with incompatible sharing rules."
      *>
      *> The three legs, and where each expected value comes from:
      *>
      *> L1 — two connectors that wrote NO clause, one INPUT and one EXTEND.
      *> Their sharing mode is 9.1.15's implementor default, which COBOL.NET
      *> has not determined (kb/Work PB322), so the arbitration reports a
      *> conflict only where EVERY candidate mode gives Table 19 an
      *> "Unsuccessful open" — and the ALL OTHER candidate gives
      *> "Normal open" for EXTEND against INPUT, so there is no conflict and
      *> 14.9.27.4 GR1 with 9.1.13.2 makes both opens '00'. The WRITE is '00'
      *> (14.9.51.4 GR12, "The successful execution of a WRITE statement
      *> releases a logical record to the operating environment"). The
      *> INPUT connector's second READ delivers the SECOND record: 9.1.12
      *> makes the file position indicator that connector's own, and the
      *> arrival of a sibling does not move it.
      *>
      *> L2 — the control that the arbiter itself is untouched: against a
      *> connector open SHARING WITH NO OTHER, a second OPEN is unsuccessful
      *> with '61' — 9.1.13.9 1) a), "An attempt is made to open a physical
      *> file that is currently open by another file connector in the sharing
      *> with no other mode".
      *>
      *> L3 — two clause-less connectors both open EXTEND, permitted by the
      *> same reading as L1. 14.9.51.4 GR19: "If two or more file connectors
      *> for a sequential file add records by sharing the physical file after
      *> opening it in extend mode, the added records follow the records
      *> present in the physical file when it was opened, but are otherwise
      *> in an undefined order" — so BOTH records shall be in the file and
      *> only their relative order is undefined, which is why this leg counts
      *> and searches instead of reading positionally.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-S ASSIGN TO "pb740shr.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS S-ST.
           SELECT F-A ASSIGN TO "pb740shr.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS A-ST.
           SELECT F-B ASSIGN TO "pb740shr.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS B-ST.
           SELECT F-X ASSIGN TO "pb740shr.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS X-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F-S.
       01 S-REC PIC X(4).
       FD F-A.
       01 A-REC PIC X(4).
       FD F-B.
       01 B-REC PIC X(4).
       FD F-X.
       01 X-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 S-ST   PIC XX.
       01 A-ST   PIC XX.
       01 B-ST   PIC XX.
       01 X-ST   PIC XX.
       01 N      PIC 9 VALUE 0.
       01 SEEN-P PIC X VALUE "N".
       01 SEEN-Q PIC X VALUE "N".
       PROCEDURE DIVISION.
           OPEN OUTPUT F-S
           MOVE "AAAA" TO S-REC
           WRITE S-REC
           MOVE "BBBB" TO S-REC
           WRITE S-REC
           CLOSE F-S

      *> L1 — the pair Table 19 permits and the host used to refuse.
           OPEN INPUT F-A
           DISPLAY "L1-A=" A-ST
           READ F-A
           DISPLAY "L1-R1=" A-REC " " A-ST
           OPEN EXTEND F-B
           DISPLAY "L1-B=" B-ST
           MOVE "CCCC" TO B-REC
           WRITE B-REC
           DISPLAY "L1-W=" B-ST
           READ F-A
           DISPLAY "L1-R2=" A-REC " " A-ST
           CLOSE F-B
           CLOSE F-A

      *> L2 — SHARING WITH NO OTHER is still exclusive to the arbiter.
           OPEN INPUT F-X
           DISPLAY "L2-X=" X-ST
           OPEN INPUT F-A
           DISPLAY "L2-A=" A-ST
           CLOSE F-X

      *> L3 — two appenders on one physical file; neither record is lost.
           OPEN EXTEND F-A
           OPEN EXTEND F-B
           DISPLAY "L3-A=" A-ST " L3-B=" B-ST
           MOVE "PPPP" TO A-REC
           WRITE A-REC
           MOVE "QQQQ" TO B-REC
           WRITE B-REC
           DISPLAY "L3-WA=" A-ST " L3-WB=" B-ST
           CLOSE F-A
           CLOSE F-B

           OPEN INPUT F-S
           PERFORM 10 TIMES
               READ F-S
                   AT END EXIT PERFORM
               END-READ
               ADD 1 TO N
               IF S-REC = "PPPP" MOVE "Y" TO SEEN-P END-IF
               IF S-REC = "QQQQ" MOVE "Y" TO SEEN-Q END-IF
           END-PERFORM
           CLOSE F-S
           DISPLAY "N=" N " P=" SEEN-P " Q=" SEEN-Q
           STOP RUN.
