      *>--- conformance-case
      *> id: pb5090-use-file-twice-position
      *> rules: [SR-14.9.49.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5090 - 1985:VIII.4.8.3 SR3: "The same file-name must not appear in more than one USE AFTER EXCEPTION statement within
      *> the same Procedure Division." (cite.py: OK; 2023:14.9.49.3 SR8 says the same). RFILE1 is named by the USE statement of ERR-A and
      *> again by ERR-B's. The refusal stands at the file-name that REPEATS it, not at the ERR-B section header.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5090D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFILE1 ASSIGN TO "PB5090D.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  RFILE1.
       01  RREC PIC X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-A SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RFILE1.
       ERR-A-PARA.
           DISPLAY "ERROR A".
       ERR-B SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RFILE1.
       ERR-B-PARA.
           DISPLAY "ERROR B".
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           STOP RUN.
