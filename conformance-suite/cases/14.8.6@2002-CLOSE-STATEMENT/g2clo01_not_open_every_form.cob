      *>--- conformance-case
      *> id: g2clo01_not_open_every_form
      *> rules: [2002-GR-14.8.6.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.3 GR1 -- "The file connector referenced by
      *> file-name-1 shall be open. If the file connector is not open,
      *> the CLOSE statement is unsuccessful and the I-O status
      *> indicator for the file connector is set to '42'." (§2002:9.1.12.6
      *> 42: "A CLOSE or UNLOCK statement is attempted for a file
      *> connector that is not in an open mode.") The rule holds for
      *> every alternative of the 2002 general format, including WITH
      *> LOCK, which later editions removed. §2002:14.8.6.2 SR2: REEL
      *> and UNIT are equivalent. §2002:9.1.12: a 42 status is fatal
      *> and the implementor decides whether execution continues;
      *> DOC-A.1-103: it continues, because the file has a FILE STATUS
      *> clause and EC-I-O checking is not enabled.
      *>   S never opened: CLOSE, WITH NO REWIND, REEL, UNIT FOR REMOVAL,
      *>   WITH LOCK                                 C1..C5 = 42
      *>   T opened, CLOSE T WITH LOCK succeeds        C6=00
      *>   (§2002:14.8.6.3 GR8: no longer in an open mode), so
      *>   CLOSE T WITH LOCK and CLOSE T are unsuccessful C7=42 C8=42
      *> ST is set to XX before each CLOSE so every value is fresh.
      *> Fails if any not-open CLOSE form reports other than 42, or the
      *> successful CLOSE WITH LOCK leaves T in an open mode.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2L1M.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "g2l1s.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST.
           SELECT T ASSIGN TO "g2l1t.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST.
       DATA DIVISION.
       FILE SECTION.
       FD S.
       01 S-REC PIC X(4).
       FD T.
       01 T-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           MOVE "XX" TO ST
           CLOSE S
           DISPLAY "C1=" ST
           MOVE "XX" TO ST
           CLOSE S WITH NO REWIND
           DISPLAY "C2=" ST
           MOVE "XX" TO ST
           CLOSE S REEL
           DISPLAY "C3=" ST
           MOVE "XX" TO ST
           CLOSE S UNIT FOR REMOVAL
           DISPLAY "C4=" ST
           MOVE "XX" TO ST
           CLOSE S WITH LOCK
           DISPLAY "C5=" ST
           OPEN OUTPUT T
           MOVE "TTTT" TO T-REC
           WRITE T-REC
           MOVE "XX" TO ST
           CLOSE T WITH LOCK
           DISPLAY "C6=" ST
           MOVE "XX" TO ST
           CLOSE T WITH LOCK
           DISPLAY "C7=" ST
           MOVE "XX" TO ST
           CLOSE T
           DISPLAY "C8=" ST
           STOP RUN.
