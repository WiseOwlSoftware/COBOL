      *>--- conformance-case
      *> id: g2rd01_read_into_own_record_area
      *> rules: [2002-SR-14.8.29.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.29.2 SR2 - "The storage area associated with
      *>   identifier-1 and the record area associated with file-name-1
      *>   shall not be the same storage area."
      *> §2002:14.8.29.3 GR3 - the records of one file "automatically
      *>   share the same record area in storage": R2, the SECOND
      *>   record of G2F, is G2F's record area as much as R1 is, so
      *>   READ G2F INTO R2 is refused at the READ line. (The INTO
      *>   phrase itself is allowed here by §2002:14.8.29.2 SR1 b: R1,
      *>   R2 and the receiver are all alphanumeric elementary items,
      *>   so the only rule the statement breaks is SR2.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2RD01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G2F ASSIGN TO "G2RD01.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  G2F.
       01  R1 PIC X(4).
       01  R2 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT G2F
           READ G2F INTO R2
               AT END DISPLAY "EOF"
           END-READ
           CLOSE G2F
           STOP RUN.
