      *>--- conformance-case
      *> id: pb5412-rewrite-from-record-area-refmod
      *> rules: [GR-14.9.35.4-7]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5412 - 1985:VII.4.5.3 SR1: "Record-name-1 and identifier-1
      *>   must not reference the same storage area." R1 (1:10) of the
      *>   10-character record R1 is that storage under another name
      *>   (cite.py: OK); refused COBOL6827 at 85 and 2002 like REWRITE
      *>   R1 FROM R1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5412N1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5412N1.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(10).
       PROCEDURE DIVISION.
       P1.
           OPEN I-O F1.
           READ F1 AT END CONTINUE END-READ.
           REWRITE R1 FROM R1 (1:10).
           CLOSE F1.
           STOP RUN.
