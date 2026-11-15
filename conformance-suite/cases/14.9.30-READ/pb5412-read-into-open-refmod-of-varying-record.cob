      *>--- conformance-case
      *> id: pb5412-read-into-open-refmod-of-varying-record
      *> rules: [GR-14.9.30.4-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5412 - 1985:VII.4.4.3 SR1: "The storage area associated
      *>   with identifier-1 and the record area associated with file-name-1
      *>   must not be the same storage area." RR (1:) is RR whatever
      *>   length its OCCURS DEPENDING ON table gives it (2023:8.4.3.3.4
      *>   GR5c); refused COBOL6827 at 85 and 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5412N6.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5412N6.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  RR.
           05 RN PIC 99.
           05 RDAT PIC X OCCURS 1 TO 8 TIMES DEPENDING ON RN.
       PROCEDURE DIVISION.
       P1.
           OPEN INPUT F1.
           READ F1 INTO RR (1:) AT END CONTINUE END-READ.
           CLOSE F1.
           STOP RUN.
