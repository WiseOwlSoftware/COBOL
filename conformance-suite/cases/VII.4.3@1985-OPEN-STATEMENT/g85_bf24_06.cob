      *>--- conformance-case
      *> id: g85_bf24_06
      *> rules: [1985-GR-VII.4.3.4-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR3 - "When a file is not in an open mode, no
      *> statement may be executed which references the file ...
      *> except for a MERGE statement with the USING or GIVING phrase,
      *> an OPEN statement, or a SORT statement with the USING or
      *> GIVING phrase." Every exempt form is executed on sequential
      *> files that are NOT open:
      *>   OPEN OUTPUT F -> 00 (§1985:VII.1.3.5 successful completion).
      *>   SORT ... USING F GIVING G with F and G closed; SORT leaves
      *>   both not open (§1985:XI.4.4.4 GR6), so OPEN INPUT F and
      *>   OPEN INPUT G each give 00 (an open file gives 41), and G
      *>   holds A001 B002 C003 in ascending order.
      *>   MERGE ... USING G H GIVING F with all three closed
      *>   (§1985:XI.4.1.4 GR7); F then holds the five keys in
      *>   ascending order, then the at end condition (F-EOF).
      *> Last, the forbidden form: a WRITE on F, which is not open.
      *> §1985:VII.1.3.5 4g sets I-O status 48, shown by the USE
      *> procedure (USE-F=48). A 4x status is critical, so what
      *> follows the USE procedure is the implementor's; nothing after
      *> it is displayed.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2406.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "g85bf2406f.dat"
               FILE STATUS IS ST-F.
           SELECT G ASSIGN TO "g85bf2406g.dat"
               FILE STATUS IS ST-G.
           SELECT H ASSIGN TO "g85bf2406h.dat"
               FILE STATUS IS ST-H.
           SELECT SW ASSIGN TO "g85bf2406w.tmp".
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(4).
       FD  G.
       01  G-REC PIC X(4).
       FD  H.
       01  H-REC PIC X(4).
       SD  SW.
       01  SW-REC.
           05 SW-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST-F PIC XX.
       01  ST-G PIC XX.
       01  ST-H PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       F-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F.
       F-ERR-PARA.
           DISPLAY "USE-F=" ST-F.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT F.
           DISPLAY "OPEN-F=" ST-F.
           MOVE "C003" TO F-REC.
           WRITE F-REC.
           MOVE "A001" TO F-REC.
           WRITE F-REC.
           MOVE "B002" TO F-REC.
           WRITE F-REC.
           CLOSE F.
           OPEN OUTPUT H.
           MOVE "A000" TO H-REC.
           WRITE H-REC.
           MOVE "B005" TO H-REC.
           WRITE H-REC.
           CLOSE H.
           SORT SW ON ASCENDING KEY SW-KEY USING F GIVING G.
           OPEN INPUT F.
           DISPLAY "OPEN-F2=" ST-F.
           CLOSE F.
           OPEN INPUT G.
           DISPLAY "OPEN-G=" ST-G.
           READ G AT END DISPLAY "G-EOF-EARLY".
           DISPLAY "G1=" G-REC.
           READ G AT END DISPLAY "G-EOF-EARLY".
           DISPLAY "G2=" G-REC.
           READ G AT END DISPLAY "G-EOF-EARLY".
           DISPLAY "G3=" G-REC.
           CLOSE G.
           MERGE SW ON ASCENDING KEY SW-KEY USING G H GIVING F.
           OPEN INPUT F.
           DISPLAY "OPEN-F3=" ST-F.
           READ F AT END DISPLAY "F-EOF-EARLY".
           DISPLAY "F1=" F-REC.
           READ F AT END DISPLAY "F-EOF-EARLY".
           DISPLAY "F2=" F-REC.
           READ F AT END DISPLAY "F-EOF-EARLY".
           DISPLAY "F3=" F-REC.
           READ F AT END DISPLAY "F-EOF-EARLY".
           DISPLAY "F4=" F-REC.
           READ F AT END DISPLAY "F-EOF-EARLY".
           DISPLAY "F5=" F-REC.
           READ F AT END DISPLAY "F-EOF".
           CLOSE F.
           WRITE F-REC.
           STOP RUN.
