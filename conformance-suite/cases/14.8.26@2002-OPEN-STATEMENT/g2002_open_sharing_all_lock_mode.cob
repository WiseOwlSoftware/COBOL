      *>--- conformance-case
      *> id: g2002_open_sharing_all_lock_mode
      *> rules: [2002-SR-14.8.26.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.26.2 SR6 - "If the SHARING phrase is omitted from the
      *> OPEN statement and the ALL phrase is specified in the SHARING
      *> clause of the file control entry for file-name-1 or if the ALL
      *> phrase is specified on the OPEN statement, the LOCK MODE clause
      *> shall be specified in the file control entry for file-name-1."
      *> This program is LEGAL under SR6 in all three of its arms, so
      *> that it compiles is the first assertion (an over-broad screen
      *> fails it):
      *>   F-A  clause ALL OTHER, LOCK MODE present, OPEN with the phrase
      *>        omitted: disjunct 1 holds and its consequent is met.
      *>   F-B  no SHARING clause, LOCK MODE present, OPEN SHARING WITH
      *>        ALL OTHER: disjunct 2 holds and its consequent is met.
      *>   F-C  clause ALL OTHER, NO LOCK MODE, OPEN SHARING WITH READ
      *>        ONLY: the phrase is present and is not ALL, so neither
      *>        disjunct holds and SR6 asks nothing.
      *> The statuses show the phrases took effect:
      *>   W=00     F-W creates the file (§2002:14.8.26.3 GR16).
      *>   A=00     F-A opens INPUT, sharing with all other.
      *>   B=00     F-B opens INPUT SHARING WITH ALL OTHER while F-A holds
      *>            it with all other / input: Table 20, Normal open.
      *>   B-REC    F-B reads the record F-W wrote.
      *>   C=00     F-C opens INPUT SHARING WITH READ ONLY on a file no
      *>            other connector holds.
      *>   D1=61    F-D (clause ALL OTHER, LOCK MODE) opens I-O while F-C
      *>            holds the file with read only: Table 20 row SHARING
      *>            WITH ALL OTHER / I-O, column read only / input is
      *>            Unsuccessful open, and §2002:9.1.12.8 item 1 c) is '61'.
      *>            Had F-C's phrase not overridden its clause (GR22: "The
      *>            SHARING phrase overrides any SHARING clause"), F-C would
      *>            hold all other / input and the open would be Normal (00).
      *>   D2=00    with F-C closed, the same open succeeds.
      *> The 61 rests on the files being shareable (§2002:14.8.26.3 GR20
      *> makes the phrase effective only for shareable files); disk files
      *> are shareable in this implementation.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OPN03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-W ASSIGN TO "g2opn03.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH NO OTHER
               FILE STATUS IS WS-W.
           SELECT F-A ASSIGN TO "g2opn03.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS WS-A.
           SELECT F-B ASSIGN TO "g2opn03.dat"
               ORGANIZATION IS SEQUENTIAL
               LOCK MODE IS AUTOMATIC
               FILE STATUS IS WS-B.
           SELECT F-C ASSIGN TO "g2opn03.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               FILE STATUS IS WS-C.
           SELECT F-D ASSIGN TO "g2opn03.dat"
               ORGANIZATION IS SEQUENTIAL
               SHARING WITH ALL OTHER
               LOCK MODE IS MANUAL
               FILE STATUS IS WS-D.
       DATA DIVISION.
       FILE SECTION.
       FD F-W.
       01 R-W PIC X(5).
       FD F-A.
       01 R-A PIC X(5).
       FD F-B.
       01 R-B PIC X(5).
       FD F-C.
       01 R-C PIC X(5).
       FD F-D.
       01 R-D PIC X(5).
       WORKING-STORAGE SECTION.
       01 WS-W PIC XX.
       01 WS-A PIC XX.
       01 WS-B PIC XX.
       01 WS-C PIC XX.
       01 WS-D PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F-W
           DISPLAY "W=" WS-W
           MOVE "HELLO" TO R-W
           WRITE R-W
           CLOSE F-W
           OPEN INPUT F-A
           DISPLAY "A=" WS-A
           OPEN INPUT SHARING WITH ALL OTHER F-B
           DISPLAY "B=" WS-B
           READ F-B AT END MOVE "EOF" TO R-B END-READ
           DISPLAY "B-REC=" R-B
           CLOSE F-B
           CLOSE F-A
           OPEN INPUT SHARING WITH READ ONLY F-C
           DISPLAY "C=" WS-C
           OPEN I-O F-D
           DISPLAY "D1=" WS-D
           CLOSE F-C
           OPEN I-O F-D
           DISPLAY "D2=" WS-D
           CLOSE F-D
           STOP RUN.
