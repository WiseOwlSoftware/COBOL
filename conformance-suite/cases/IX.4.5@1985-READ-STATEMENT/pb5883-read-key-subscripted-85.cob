      *>--- conformance-case
      *> id: pb5883-read-key-subscripted-85
      *> rules: [1985-SR-IX.4.5.3-2, 1985-SR-IX.4.5.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5883. "Data-name-1 may be qualified" (§1985:IX.4.5.3
      *> SR3) and nothing more, and "Each table element reference must
      *> be subscripted" (§1985:IV.4.3.8.2.3 SR4), so a table element is
      *> never data-name-1 and never "a data item specified as a record
      *> key" (SR2), though IX-T lies at the prime key's positions in a
      *> second record of the file. WiseOwl refuses the READ with
      *> COBOL2024; a compiler that accepts it fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5883S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb5883s.dat"
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
          05 IX-T PIC X(4) OCCURS 2 TIMES.
          05 FILLER  PIC X(1).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IXF.
           MOVE "BBBB" TO IX-T (1).
           READ IXF KEY IS IX-T (1)
               INVALID KEY DISPLAY "INV-1"
               NOT INVALID KEY DISPLAY "R1 " IX-DATA
           END-READ.
           CLOSE IXF.
           STOP RUN.
