      *>--- conformance-case
      *> id: g2-format-from-index-item
      *> rules: [2002-SR-13.16.22.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.22.2 SR3 - "Data items of class index, object, and
      *> pointer shall not be specified in a record description entry
      *> associated with a file description entry in which a FORMAT
      *> clause is specified or in the data description entry of the
      *> identifier specified in a FROM phrase in a REWRITE or WRITE
      *> statement referencing that file description entry."
      *> F's own record holds no such item; the FROM identifier
      *> WS-SRC holds the USAGE INDEX item WS-IX: refused at the WRITE.
      *> Why it can fail: the screen covers the record description
      *> entries and not the FROM operand.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FMT02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G2FMT02.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F RECORD IS VARYING IN SIZE FROM 1 TO 40 CHARACTERS
           FORMAT CHARACTER DATA.
       01  F-REC.
           05  F-TEXT PIC X(12).
       WORKING-STORAGE SECTION.
       01  WS-SRC.
           05  WS-TXT PIC X(4).
           05  WS-IX  USAGE INDEX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F.
           WRITE F-REC FROM WS-SRC.
           CLOSE F.
           STOP RUN.
