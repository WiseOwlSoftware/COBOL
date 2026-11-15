      *>--- conformance-case
      *> id: g2clo06_absent_optional_lock
      *> rules: [2002-GR-14.8.6.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.6.3 GR6 -- when the file position indicator shows
      *> "that an optional input file is not present, no end-of-file
      *> or unit processing is performed for the file and the file
      *> position indicator and the current volume pointer are
      *> unchanged". Exercised with CLOSE UNIT and with the 2002-only
      *> CLOSE WITH LOCK.
      *> OF1: OPEN INPUT of an absent OPTIONAL file is 05
      *>   (§2002:9.1.12.1 item 4) and sets the indicator to "optional
      *>   input file is not present" (§2002:14.8.26.3 GR11).
      *>   CLOSE OF1 UNIT: no unit processing, the indicator unchanged
      *>   and the file stays open; its status is successful (class 0;
      *>   whether 00 or 07 is not pinned). The next READ still sees the
      *>   not-present indicator: 10 (§2002:14.8.29.3 GR18 b;
      *>   §2002:9.1.12.3 item 1 c) -- 47 if the CLOSE UNIT had closed
      *>   it.
      *>   CLOSE OF1 WITH LOCK: no end-of-file processing, successful,
      *>   00; the lock still applies (GR3 symbol e), so the re-OPEN is
      *>   38 (§2002:14.8.26.3 GR24).
      *> OF2: the control -- a plain CLOSE of an absent OPTIONAL file,
      *>   00, and the re-OPEN is 05 again (no lock), so RELOCKED=38 is
      *>   the LOCK phrase's doing and not the absent file's.
      *> DOC-A.1-103: execution continues after the 38 because the file
      *> has a FILE STATUS clause and EC-I-O checking is not enabled.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CL6O.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL OF1 ASSIGN TO "g2c6none1.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST1.
           SELECT OPTIONAL OF2 ASSIGN TO "g2c6none2.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS ST2.
       DATA DIVISION.
       FILE SECTION.
       FD OF1.
       01 R1 PIC X(4).
       FD OF2.
       01 R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01 ST1 PIC XX.
       01 ST2 PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT OF1
           DISPLAY "OPEN1=" ST1
           CLOSE OF1 UNIT
           DISPLAY "UNIT-CLASS=" ST1(1:1)
           READ OF1 AT END CONTINUE END-READ
           DISPLAY "READ=" ST1
           CLOSE OF1 WITH LOCK
           DISPLAY "LOCK=" ST1
           OPEN INPUT OF1
           DISPLAY "RELOCKED=" ST1
           OPEN INPUT OF2
           DISPLAY "OPEN2=" ST2
           CLOSE OF2
           DISPLAY "CLOSE2=" ST2
           OPEN INPUT OF2
           DISPLAY "REOPEN2=" ST2
           CLOSE OF2
           STOP RUN.
