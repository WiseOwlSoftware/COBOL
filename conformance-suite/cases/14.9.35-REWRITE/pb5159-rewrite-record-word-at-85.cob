      *>--- conformance-case
      *> id: pb5159-rewrite-record-word-at-85
      *> rules: [FMT-14.9.35.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5159 - the edition half of pb5159_rewrite_record_word_2002. §1985:VII.4.5.2 prints
      *> REWRITE record-name-1 [FROM identifier-1] with no word RECORD. Expected: COBOL0900 at 85, at RECORD.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5159NRW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5159NRW"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(5).
       WORKING-STORAGE SECTION.
       01  W PIC X(5) VALUE "NEW01".
       PROCEDURE DIVISION.
       P1.
           OPEN I-O F.
           REWRITE R RECORD FROM W.
           CLOSE F.
           STOP RUN.
