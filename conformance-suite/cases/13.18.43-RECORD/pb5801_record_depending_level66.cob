      *>--- conformance-case
      *> id: pb5801_record_depending_level66
      *> rules: [SR-13.18.43.3-6]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §2023:13.18.43.3 SR6: "Data-name-1 shall describe
      *>   an elementary unsigned integer in the working-storage" (or
      *>   local-storage or linkage) section. LN66 RENAMES the PIC 99 item
      *>   G-LN, whose attributes it has (§2023:13.18.45.4 GR1): legal.
      *> DERIVATION. §2023:13.18.43.4 GR11: "the number of bytes in the
      *>   record shall be placed into the data item referenced by
      *>   data-name-1 before any RELEASE, REWRITE, or WRITE statement", so
      *>   the records written are ABC (3) and VWXYZ (5). GR15: after a
      *>   READ "the contents of the data item referenced by data-name-1
      *>   will indicate the number" of bytes in the record just read:
      *>   G-LN is set to 03, then 05.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801), or a record length is taken from or stored into
      *>   anywhere but G-LN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801R2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO P5801R2.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           RECORD IS VARYING IN SIZE FROM 1 TO 10 CHARACTERS
               DEPENDING ON LN66.
       01  R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01  G1.
           02  G-LN PIC 99.
       66  LN66 RENAMES G-LN.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           MOVE "ABCDEFGHIJ" TO R1.
           MOVE 3 TO LN66.
           WRITE R1.
           MOVE "VWXYZ" TO R1.
           MOVE 5 TO G-LN.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           MOVE 0 TO G-LN.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "LEN " G-LN " " R1 (1:LN66).
           MOVE 0 TO G-LN.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "LEN " G-LN " " R1 (1:LN66).
           CLOSE F1.
           STOP RUN.
