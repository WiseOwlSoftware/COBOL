      *>--- conformance-case
      *> id: g2mrg-key-beyond-minimum-record
      *> rules: [2002-SR-14.8.23.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.23.2 SR4g - "If the file referenced by file-name-1
      *>   contains variable-length records, all the data items
      *>   identified by key data-names shall be contained within the
      *>   first x bytes of the record, where x equals the minimum
      *>   record size specified for the file referenced by
      *>   file-name-1." SW varies from 4 to 8 characters and the key
      *>   SB occupies bytes 5..8, beyond the minimum 4: the MERGE is
      *>   refused at the MERGE line. Every other rule holds: SR3 (the
      *>   8-byte USING records lie within 4..8), SR4a (SB is in SW's
      *>   record), SR4c/d/f (alphanumeric, fixed length, no OCCURS).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MRGKG.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "g2mrgkg.tmp".
           SELECT MI1 ASSIGN TO "g2mrgkg1.dat".
           SELECT MI2 ASSIGN TO "g2mrgkg2.dat".
           SELECT MO1 ASSIGN TO "g2mrgkgo.dat".
       DATA DIVISION.
       FILE SECTION.
       SD SW RECORD IS VARYING IN SIZE FROM 4 TO 8 CHARACTERS.
       01 SR.
          05 SA PIC X(4).
          05 SB PIC X(4).
       FD MI1.
       01 M1R PIC X(8).
       FD MI2.
       01 M2R PIC X(8).
       FD MO1.
       01 MOR PIC X(8).
       PROCEDURE DIVISION.
       M-0.
           MERGE SW ASCENDING KEY SB USING MI1 MI2 GIVING MO1
           STOP RUN.
