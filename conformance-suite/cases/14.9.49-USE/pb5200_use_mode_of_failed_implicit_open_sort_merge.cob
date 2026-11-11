      *>--- conformance-case
      *> id: pb5200_use_mode_of_failed_implicit_open_sort_merge
      *> rules: [GR-14.9.49.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5200 -- the implicit OPEN of a SORT or MERGE USING file is an OPEN INPUT of that file, so when it
      *> fails the open-mode procedure of the INPUT mode runs: the file is "in the process of being opened in the
      *> input mode" (§2023:14.9.49.4 GR6 b); §1985:VII.4.6.4 GR5 b)), and once the statement ends the file is in
      *> no mode, so a later READ of it is refused and applies no procedure.
      *> DERIVATION -- every expected line follows from the rules, nothing from the compiler:
      *>  - FJ is created empty (OPEN OUTPUT of an absent file succeeds, status 00, no procedure; CLOSE 00).
      *>  - SORT ... USING FI: FI is absent, so the implicit OPEN INPUT is unsuccessful with status 35
      *>    (§2023:14.9.27.4 Table 18, INPUT row); the INPUT procedure runs after the status is set: "USE INPUT 35".
      *>  - READ FI: FI is not open: status 47 (§2023:9.1.13.7), and no procedure applies: FI is in no mode.
      *>  - MERGE ... USING FI FJ: FI's implicit OPEN INPUT fails again (35), the INPUT procedure runs once;
      *>    FJ opens successfully, so the one line holds whichever order the files are opened in.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5200D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FI ASSIGN TO "pb5200d-in1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST.
           SELECT FJ ASSIGN TO "pb5200d-in2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS SJ.
           SELECT FO ASSIGN TO "pb5200d-out.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SW ASSIGN TO "pb5200d-work".
       DATA DIVISION.
       FILE SECTION.
       FD  FI.
       01  IR PIC X(4).
       FD  FJ.
       01  JR PIC X(4).
       FD  FO.
       01  OR1 PIC X(4).
       SD  SW.
       01  SR PIC X(4).
       WORKING-STORAGE SECTION.
       01  ST PIC XX.
       01  SJ PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       UI SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON INPUT.
       UI-P.
           DISPLAY "USE INPUT " ST.
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT FJ.
           CLOSE FJ.
           SORT SW ON ASCENDING KEY SR USING FI GIVING FO.
           DISPLAY "AFTER SORT".
           READ FI AT END DISPLAY "AT END".
           DISPLAY "READ " ST.
           MERGE SW ON ASCENDING KEY SR USING FI FJ GIVING FO.
           DISPLAY "AFTER MERGE".
           STOP RUN.
