      *>--- conformance-case
      *> id: pb5883-read-key-qualified-85
      *> rules: [1985-SR-IX.4.5.3-2, 1985-SR-IX.4.5.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5883, the control. "Data-name-1 may be qualified"
      *> (§1985:IX.4.5.3 SR3), and IX-KEY OF IX-REC is the data item
      *> specified as the record key (SR2), so the READ is accepted.
      *> Records AAAA/ONE and BBBB/TWO are written; the READ by the
      *> key BBBB makes that record available (§1985:IX.4.5.4 GR17),
      *> so the program prints R1 TWO. The leg fails if the qualified
      *> operand is refused or the read finds another record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5883Q.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb5883q.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(4).
          05 IX-DATA PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IXF.
           MOVE "AAAA" TO IX-KEY. MOVE "ONE" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W1".
           MOVE "BBBB" TO IX-KEY. MOVE "TWO" TO IX-DATA.
           WRITE IX-REC INVALID KEY DISPLAY "W2".
           CLOSE IXF.
           OPEN INPUT IXF.
           MOVE "BBBB" TO IX-KEY.
           READ IXF KEY IS IX-KEY OF IX-REC
               INVALID KEY DISPLAY "INV-1"
               NOT INVALID KEY DISPLAY "R1 " IX-DATA
           END-READ.
           CLOSE IXF.
           STOP RUN.
