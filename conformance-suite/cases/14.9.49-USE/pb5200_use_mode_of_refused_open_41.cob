      *>--- conformance-case
      *> id: pb5200_use_mode_of_refused_open_41
      *> rules: [GR-14.9.49.4-6]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5200 -- an OPEN refused with I-O status 41 (the file connector is already open) is still an OPEN
      *> attempted in ITS mode: the open-mode procedure applicable to that mode runs, never the one of the mode the
      *> file is open in (§2023:14.9.49.4 GR6 b)-e), "open in the <mode> mode or in the process of being opened in the
      *> <mode> mode"; §1985:VII.4.6.4 GR5 b)-e)).
      *> DERIVATION -- every expected line follows from the rules, nothing from the compiler:
      *>  - OPEN OUTPUT of an absent file creates it (§2023:14.9.27.4 Table 18, OUTPUT row): status 00, no procedure.
      *>    WRITE 00, CLOSE 00. OPEN INPUT of the file now present: 00, no procedure.
      *>  - Each further OPEN names a file connector that is open: unsuccessful, status 41 (§2023:14.9.27.4 GR2,
      *>    §2023:9.1.13.7 item 1), the file unaffected (§2023:14.9.27.4 GR25). The procedure of the mode being opened runs after the
      *>    status is set (§2023:14.9.27.4 GR25 a), b)): OUTPUT, then EXTEND, then I-O. The INPUT procedure, the mode the file is open
      *>    in, is not applicable to these OPENs: the file is not being opened in the input mode.
      *>  - READ of the still-open input file is successful (status 00), no procedure; CLOSE 00.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5200B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FB ASSIGN TO "pb5200b.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  FB.
       01  BR PIC X(4).
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
           OPEN OUTPUT FB.
           MOVE "ABCD" TO BR.
           WRITE BR.
           CLOSE FB.
           OPEN INPUT FB.
           DISPLAY "OPEN INPUT " ST.
           OPEN OUTPUT FB.
           DISPLAY "OPEN OUTPUT WHILE OPEN " ST.
           OPEN EXTEND FB.
           DISPLAY "OPEN EXTEND WHILE OPEN " ST.
           OPEN I-O FB.
           DISPLAY "OPEN I-O WHILE OPEN " ST.
           READ FB.
           DISPLAY "READ " ST.
           CLOSE FB.
           DISPLAY "CLOSE " ST.
           STOP RUN.
