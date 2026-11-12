      *>--- conformance-case
      *> id: pb5159_rewrite_record_word_2002
      *> rules: [FMT-14.9.35.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5159 - the optional word RECORD after REWRITE's record-name. §2023:14.9.35.2 prints
      *> REWRITE {record-name-1 | FILE file-name-1} RECORD [FROM ...], RECORD not underlined (the ISO/IEC 1989:2002
      *> format 14.8.33.1 prints the same). The record OLD01 is read back, rewritten from W ("NEW01") and read again:
      *> NEW01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5159RW.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "PB5159RW"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  R PIC X(5).
       WORKING-STORAGE SECTION.
       01  W PIC X(5) VALUE "NEW01".
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           MOVE "OLD01" TO R.
           WRITE R.
           CLOSE F.
           OPEN I-O F.
           READ F AT END DISPLAY "EOF" END-READ.
           REWRITE R RECORD FROM W.
           CLOSE F.
           OPEN INPUT F.
           READ F AT END DISPLAY "EOF" END-READ.
           DISPLAY R.
           CLOSE F.
           STOP RUN.
