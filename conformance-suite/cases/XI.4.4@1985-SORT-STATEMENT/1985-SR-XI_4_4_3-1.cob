      *>--- conformance-case
      *> id: 1985-SR-XI_4_4_3-1
      *> rules: [1985-SR-XI.4.4.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XI.4.4.3 SR1 (THE SORT STATEMENT). A refusal: the rule
      *> forbids line 23 of the program below, and WiseOwl refuses it at
      *> 85 with COBOL2700 there; a compiler that accepts it fails.
      *> Placed from the confirmation pass's redraft (WRONG original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SRTDCL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IN-FILE ASSIGN TO INFILE.
           SELECT OUT-FILE ASSIGN TO OUTFILE.
           SELECT SRT-FILE ASSIGN TO SRTWORK.
       DATA DIVISION.
       FILE SECTION.
       FD  IN-FILE LABEL RECORDS ARE STANDARD.
       01  IN-REC PIC X(10).
       FD  OUT-FILE LABEL RECORDS ARE STANDARD.
       01  OUT-REC PIC X(10).
       SD  SRT-FILE.
       01  SRT-REC.
           05 SRT-KEY PIC X(10).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IN-FILE.
       ERR-PARA.
           SORT SRT-FILE ON ASCENDING KEY SRT-KEY
               USING IN-FILE GIVING OUT-FILE.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           DISPLAY "DONE".
           STOP RUN.
