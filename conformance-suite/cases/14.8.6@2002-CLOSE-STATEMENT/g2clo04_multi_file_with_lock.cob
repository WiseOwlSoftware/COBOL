      *>--- conformance-case
      *> id: g2clo04_multi_file_with_lock
      *> rules: [2002-GR-14.8.6.3-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.3 GR10 -- a multi-file CLOSE is "the same as if a
      *> separate CLOSE statement had been written for each file-name-1
      *> in the same order", and after a declarative's RESUME ... NEXT
      *> STATEMENT "processing resumes at the next implicit CLOSE
      *> statement, if any". Written with WITH LOCK, a 2002 alternative
      *> later editions removed, so each implicit CLOSE carries its own
      *> file-name's phrase.
      *> (i) CLOSE F1 WITH LOCK F2 WITH LOCK F3 WITH LOCK, F2 never
      *>   opened: F2's implicit CLOSE is unsuccessful, 42 (GR1), and
      *>   §2002:14.8.45.3 GR6 runs its USE procedure "upon the
      *>   unsuccessful execution of an input-output operation":
      *>   D2=42; RESUME AT NEXT STATEMENT goes on to F3's CLOSE.
      *>   ST1=00 ST2=42 ST3=00; F1 and F3 were each closed with lock,
      *>   so each re-OPEN is 38 (§2002:9.1.12.5).
      *> (ii) CLOSE F4 F5 WITH LOCK: two separate CLOSEs, only the
      *>   second with LOCK: re-OPEN F4 is 00, re-OPEN F5 is 38.
      *> §2002:9.1.12: 38 is fatal and the implementor decides whether
      *> execution continues; DOC-A.1-103: it continues, because each
      *> file has a FILE STATUS clause and EC-I-O checking is not
      *> enabled.
      *> Fails if F3 is skipped after the declarative (ST3 stays XX,
      *> RE3 not 38), or a LOCK phrase spreads to or is lost from a
      *> neighbouring file-name (RE4=38 or RE5=00).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2L4M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2l4a.dat" FILE STATUS IS ST1.
           SELECT F2 ASSIGN TO "g2l4b.dat" FILE STATUS IS ST2.
           SELECT F3 ASSIGN TO "g2l4c.dat" FILE STATUS IS ST3.
           SELECT F4 ASSIGN TO "g2l4d.dat" FILE STATUS IS ST4.
           SELECT F5 ASSIGN TO "g2l4e.dat" FILE STATUS IS ST5.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(5).
       FD F2.
       01 R2 PIC X(5).
       FD F3.
       01 R3 PIC X(5).
       FD F4.
       01 R4 PIC X(5).
       FD F5.
       01 R5 PIC X(5).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       01 ST3 PIC XX.
       01 ST4 PIC XX.
       01 ST5 PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-F2 SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON F2.
       ERR-F2-P.
           DISPLAY "D2=" ST2
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN-S SECTION.
       MAIN.
           OPEN OUTPUT F1
           MOVE "AAAAA" TO R1
           WRITE R1
           OPEN OUTPUT F3
           MOVE "CCCCC" TO R3
           WRITE R3
           MOVE "XX" TO ST1 ST2 ST3
           CLOSE F1 WITH LOCK F2 WITH LOCK F3 WITH LOCK
           DISPLAY "ST1=" ST1 " ST2=" ST2 " ST3=" ST3
           OPEN INPUT F1
           DISPLAY "RE1=" ST1
           OPEN INPUT F3
           DISPLAY "RE3=" ST3
           OPEN OUTPUT F4
           OPEN OUTPUT F5
           CLOSE F4 F5 WITH LOCK
           DISPLAY "ST4=" ST4 " ST5=" ST5
           OPEN INPUT F4
           DISPLAY "RE4=" ST4
           OPEN INPUT F5
           DISPLAY "RE5=" ST5
           STOP RUN.
