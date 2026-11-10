      *>--- conformance-case
      *> id: pb5076-read-sequential-without-at-end-85
      *> rules: [GR-14.9.30.4-24]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5076 - 1985:VII.4.4.3 SR2: "The AT END phrase must be specified if no applicable USE AFTER STANDARD EXCEPTION
      *>   procedure is specified for file-name-1." (cite.py: OK). The program has no declaratives, so no procedure applies to SF and the
      *>   READ names no AT END. COBOL-2002 and later print no such rule (2023:14.9.30.4 GR24 d)).
      *>   conformance:pb5076_sequential_read_without_at_end_covered_by_use is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5076N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB5076N1.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF.
       01  SREC PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT SF.
           READ SF.
           CLOSE SF.
           STOP RUN.
