      *>--- conformance-case
      *> id: pb5794-read-key-numeric-twin
      *> rules: [SR-14.9.30.3-11]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5794 - ISO 14.9.30.3 SR11 requires the KEY operand to be a record key of file-name-1; the operand here is not.
      *> IX-NUM (PIC 9(4), class numeric) is the same width as the record key IX-KEY (PIC X(4), class
      *> alphanumeric) and begins at its character position, in a second record description of the file. The
      *> implicit key of GR4 is an item of the key's own class, category and usage, so a numeric item is no key:
      *> the positional match once compared offset and width alone and accepted it. The leg fails if it compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5794R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb5794r.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(4).
          05 IX-DATA PIC X(5).
       01 IX-REC2.
          05 IX-NUM  PIC 9(4).
          05 FILLER  PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IXF
           MOVE 2222 TO IX-NUM
           READ IXF KEY IS IX-NUM
               INVALID KEY DISPLAY "INVALID"
           END-READ
           CLOSE IXF
           STOP RUN.
