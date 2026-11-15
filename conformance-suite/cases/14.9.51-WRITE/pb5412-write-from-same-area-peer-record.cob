      *>--- conformance-case
      *> id: pb5412-write-from-same-area-peer-record
      *> rules: [GR-14.9.51.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5412 - 1985:VII.4.7.3 SR1: "Record-name-1 and identifier-1
      *>   must not reference the same storage area." With SAME AREA FOR F1
      *>   F2, "The area being shared includes all storage areas assigned
      *>   to the files" (1985:VII.2.13.4 GR1), so F2's record R2 is F1's
      *>   storage; refused COBOL6827 at 85 and 2002, as a SAME RECORD
      *>   AREA peer's record already is.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5412N5.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5412N5.DAT".
           SELECT F2 ASSIGN TO "PB5412N5.OTH".
       I-O-CONTROL.
           SAME AREA FOR F1 F2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       FD  F2.
       01  R2 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           WRITE R1 FROM R2.
           CLOSE F1.
           STOP RUN.
