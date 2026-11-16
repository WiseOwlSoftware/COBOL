      *>--- conformance-case
      *> id: pb5801_file_status_level66_85
      *> rules: [1985-SR-VII.2.5.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801. §1985:VII.2.5.3 SR2: "must be defined in the
      *>   Data Division as a two-character data item of the category
      *>   alphanumeric". FS66 RENAMES the PIC XX item G-FS, and
      *>   §1985:VI.5.11.4 GR2: "all of the data attributes of data-name-2
      *>   become the data attributes for data-name-1", so FS66 is such an
      *>   item in Working-Storage and the clause is legal.
      *> DERIVATION. §1985:VII.2.5.4 GR1: "the data item referenced by
      *>   data-name-1 is always updated to contain the value of the I-O
      *>   status". OPEN OUTPUT succeeds: 00, seen through G-FS, the
      *>   storage FS66 renames. The READ of the one record: 00. The
      *>   second READ finds no next record, the at end condition: 10.
      *> Fails if the compiler ends with an exception (it did, kb/Work
      *>   PB5801), or stores the status anywhere but G-FS.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801FS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO P5801FS
               FILE STATUS IS FS66.
       DATA DIVISION.
       FILE SECTION.
       FD  F1
           LABEL RECORDS ARE STANDARD.
       01  R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01  G1.
           02  G-FS PIC XX VALUE "ZZ".
       66  FS66 RENAMES G-FS.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT F1.
           DISPLAY "OPEN " G-FS.
           MOVE "ABCD" TO R1.
           WRITE R1.
           CLOSE F1.
           OPEN INPUT F1.
           READ F1 AT END DISPLAY "EOF".
           DISPLAY "READ " FS66 " " R1.
           READ F1 AT END DISPLAY "AT END " FS66.
           CLOSE F1.
           STOP RUN.
