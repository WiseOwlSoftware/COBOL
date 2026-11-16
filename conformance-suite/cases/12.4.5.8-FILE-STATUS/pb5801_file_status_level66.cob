      *>--- conformance-case
      *> id: pb5801_file_status_level66
      *> rules: [SR-12.4.5.8.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §2023:12.4.5.8.3 SR2: "Data-name-1 shall
      *>   reference a two-character data item of the category
      *>   alphanumeric", in working-storage here. FS66 RENAMES G-S1 THRU
      *>   G-S2, and §2023:13.18.45.4 GR2: "data-name-1 defines an
      *>   alphanumeric group item" of those two characters, so it is one.
      *> DERIVATION. §2023:12.4.5.8.4 GR1: "the data item referenced by
      *>   data-name-1 is updated to contain the value of the I-O status".
      *>   OPEN OUTPUT succeeds: 00, seen through G-S1 and G-S2, the
      *>   storage FS66 spans. The READ of the one record: 00. The second
      *>   READ finds no next record, the at end condition: 10.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801), or stores the status anywhere but G-S1 and G-S2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801F2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO P5801F2
               FILE STATUS IS FS66.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01  G1.
           02  G-S1 PIC X VALUE "Z".
           02  G-S2 PIC X VALUE "Z".
       66  FS66 RENAMES G-S1 THRU G-S2.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           DISPLAY "OPEN " G-S1 G-S2.
           MOVE "ABCD" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "READ " FS66 " " R1.
           READ F1 AT END DISPLAY "AT END " FS66.
           CLOSE F1.
           STOP RUN.
