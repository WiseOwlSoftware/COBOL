      *>--- conformance-case
      *> id: pb5200_use_mode_not_stale_after_failed_open
      *> rules: [GR-14.9.49.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5200 -- the open-mode USE procedures apply to a file "open in the <mode> mode or in the process of
      *> being opened in the <mode> mode" (§2023:14.9.49.4 GR6 b)-e); §1985:VII.4.6.4 GR5 b)-e)), and to no file
      *> after the OPEN that was attempting the mode has ended unsuccessfully.
      *> DERIVATION -- every expected line follows from the rules, nothing from the compiler:
      *>  - FI is a non-optional file no run of this test creates: an OPEN INPUT or OPEN EXTEND of it is unsuccessful
      *>    with I-O status 35 (§2023:9.1.13.6 rule 5; §2023:14.9.27.4 GR4 Table 18). The corpus runner runs each program
      *>    in a fresh empty directory.
      *>  - OPEN INPUT: the file is being opened in the input mode, so the INPUT procedure runs once (§2023:14.9.49.4 GR6 b), after the
      *>    status is set (§2023:14.9.27.4 GR25 a), b)): "USE INPUT 35", then the statement after the OPEN: "OPEN INPUT 35".
      *>  - OPEN EXTEND: likewise the EXTEND procedure (§2023:14.9.49.4 GR6 e): "USE EXTEND 35", "OPEN EXTEND 35".
      *>  - READ, WRITE and CLOSE of a file that is NOT open are unsuccessful ('47', '48', '42': §2023:9.1.13.7
      *>    items 7, 8 and 2 -- READ of a file not open in the input or I-O mode, WRITE of one not open for output or
      *>    extend, CLOSE of one not in an open mode). The file is in no mode: not open, and no OPEN is executing. No open-mode procedure applies,
      *>    exactly as for the same statements before any OPEN, so none runs after the failed OPENs. A file-name USE
      *>    procedure is not declared, so each prints just its status line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5200A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FI ASSIGN TO "pb5200a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  FI.
       01  FR PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       UI SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       UI-P.
           DISPLAY "USE INPUT " ST.
       UO SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON OUTPUT.
       UO-P.
           DISPLAY "USE OUTPUT " ST.
       UE SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON EXTEND.
       UE-P.
           DISPLAY "USE EXTEND " ST.
       UX SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON I-O.
       UX-P.
           DISPLAY "USE I-O " ST.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           READ FI.
           DISPLAY "READ BEFORE ANY OPEN " ST.
           OPEN INPUT FI.
           DISPLAY "OPEN INPUT " ST.
           READ FI.
           DISPLAY "READ AFTER FAILED OPEN " ST.
           MOVE "ABCD" TO FR.
           WRITE FR.
           DISPLAY "WRITE AFTER FAILED OPEN " ST.
           OPEN EXTEND FI.
           DISPLAY "OPEN EXTEND " ST.
           WRITE FR.
           DISPLAY "WRITE AFTER FAILED EXTEND " ST.
           CLOSE FI.
           DISPLAY "CLOSE AFTER FAILED OPEN " ST.
           STOP RUN.
