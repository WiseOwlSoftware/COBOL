      *>--- conformance-case
      *> id: pb5801_record_depending_level66_85
      *> rules: [1985-SR-VII.3.8.3-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §1985:VII.3.8.3 SR4: "Data-name-1 must describe
      *>   an elementary unsigned integer in the Working-Storage or
      *>   Linkage Section". LN66 RENAMES the PIC 99 item G-LN, whose
      *>   attributes it has (§1985:VI.5.11.4 GR2), so it is legal.
      *> DERIVATION. §1985:VII.3.8.4 GR8: "the number of character
      *>   positions in the record must be placed into the data item
      *>   referenced by data-name-1" before the WRITE, so the records
      *>   written are ABC (3) and VWXYZ (5). §1985:VII.3.8.4 GR11: after
      *>   a READ "the contents of the data item referenced by data-name-1
      *>   will indicate the number of character positions in the record
      *>   just read": G-LN is set to 03, then 05.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801), or a record length is taken from or stored into
      *>   anywhere but G-LN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801RD.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO P5801RD.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD
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
