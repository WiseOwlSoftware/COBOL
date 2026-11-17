      *>--- conformance-case
      *> id: pb5883-read-key-subscripted
      *> rules: [SR-14.9.30.3-11, SR-14.9.30.3-12]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5883. "Data-name-1 or record-key-name-1 may be qualified" (ISO 14.9.30.3 SR12), and a
      *> data-name in a general format is a qualified-data-name with no subscript (8.4.2.2.2 Format 1; at
      *> 2014 the same, 14.9.29.2 SR10). IX-T (1) lies at the prime key's positions in a second record of
      *> the file, but a subscripted table element is no data-name-1, so the READ is refused (COBOL2024)
      *> at 2014 and 2023. COBOL-2002 reads data-name-n "including qualification and subscription if
      *> needed" (2002 5.1.3, kb/Work PB5602), so it is not refused there. The leg fails if it compiles.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5883T.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb5883t.dat"
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
