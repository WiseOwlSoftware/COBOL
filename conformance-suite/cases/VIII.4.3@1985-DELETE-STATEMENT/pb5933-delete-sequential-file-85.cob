      *>--- conformance-case
      *> id: pb5933-delete-sequential-file-85
      *> rules: [1985-FMT-VIII.4.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85 that names its 1985 ground (kb/Work PB5933).
      *> COBOL-85 is built of modules, and the statements of the
      *> sequential I-O module are CLOSE, OPEN, READ, REWRITE, USE and
      *> WRITE (§1985:VII.1.2, "the Sequential I-O level 2 provides
      *> full capabilities for the CLOSE, OPEN, READ, REWRITE, USE,
      *> and WRITE statements"); "DELETE file-name-1 RECORD" belongs to
      *> the relative and indexed modules (§1985:VIII.4.3.2,
      *> §1985:IX.4.3.2). Each cite.py --check OK. F is a sequential
      *> file, so WiseOwl refuses the DELETE with COBOL0865, and its
      *> message at 85 quotes §1985:VII.1.2 where it once quoted the
      *> 2023 rule (§2023:14.9.10.3 SR1, l1-delete-record-sequential-
      *> organization).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5933A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5933a.dat"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS WS-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  F-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-ST PIC XX.
       PROCEDURE DIVISION.
       P0.
           OPEN I-O F.
           DELETE F RECORD
               INVALID KEY DISPLAY "INVALID".
           CLOSE F.
           STOP RUN.
