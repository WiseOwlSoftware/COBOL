      *>--- conformance-case
      *> id: pb5200_use_mode_of_open_refused_closed_with_lock_38
      *> rules: [1985-GR-VII.4.3.4-26]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5200 -- an OPEN of a file closed WITH LOCK is unsuccessful with I-O status 38 (§1985:VII.1.3.5
      *> "I-O Status = 38. A permanent error exists because an OPEN statement is attempted on a file previously
      *> closed with lock"), and the OPEN's USE procedures then run (§1985:VII.4.3.4 GR26 b): "Any applicable USE
      *> AFTER EXCEPTION procedure is executed"). §1985:VII.4.6.4 GR5 c) and b) make the OUTPUT and INPUT procedures
      *> applicable to a file "in the process of being opened" in that mode, so each refused OPEN runs the procedure
      *> of ITS mode, after the status is set (GR26 a).
      *> DERIVATION -- every expected line follows from the rules, nothing from the compiler:
      *>  - OPEN OUTPUT of an absent file creates it (status 00); WRITE 00; CLOSE WITH LOCK 00.
      *>  - OPEN OUTPUT: status 38, then the OUTPUT procedure: "USE OUTPUT 38", "OPEN OUTPUT 38".
      *>  - OPEN INPUT: status 38, then the INPUT procedure: "USE INPUT 38", "OPEN INPUT 38".
      *>  - READ of the file, which is not open and is being opened in no mode: 47 (§1985:VII.1.3.5), and no procedure
      *>    applies: the file is in no mode once the refused OPEN has ended, exactly as before any OPEN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5200C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FL ASSIGN TO "pb5200c.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD  FL.
       01  LR PIC X(4).
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
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           OPEN OUTPUT FL.
           MOVE "ABCD" TO LR.
           WRITE LR.
           CLOSE FL WITH LOCK.
           OPEN OUTPUT FL.
           DISPLAY "OPEN OUTPUT " ST.
           OPEN INPUT FL.
           DISPLAY "OPEN INPUT " ST.
           READ FL.
           DISPLAY "READ " ST.
           STOP RUN.
