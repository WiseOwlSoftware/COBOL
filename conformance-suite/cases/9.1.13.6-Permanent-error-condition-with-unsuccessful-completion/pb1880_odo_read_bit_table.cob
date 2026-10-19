*>--- conformance-case
*> id: pb1880_odo_read_bit_table
*> rules: [GR-9.1.13.6-4]
*> accept: [2023]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 1989:2023 §9.1.13.6 item 4 b) over an OCCURS DEPENDING ON table of USAGE BIT items: "A READ statement is
      *> unsuccessfully executed because the records are variable in length because of an OCCURS DEPENDING ON clause in
      *> the record ... and the associated DEPENDING ON item contains a value that makes the number of bytes in the
      *> record exceed the value specified or implied by the file description entry" - '34'. The unit of the comparison
      *> is the BYTE, and a record whose last item ends inside a byte still occupies that whole byte (§13.18.43.4 GR4:
      *> "If the last data item in the logical record does not end at a byte boundary, the record size includes the
      *> entire byte in which that data item ends"). The record below is the two-byte count N plus a table of 1 TO 20
      *> bits, so its size at a count N is 2 + ceil(N / 8) bytes and the file description IMPLIES the largest
      *> description, 2 + ceil(20 / 8) = 5 bytes (§13.18.43.4 GR5 a)). The record is unsuccessful exactly when
      *> 2 + ceil(N / 8) exceeds 5, i.e. when N exceeds 24:
      *>   N = 20  - 5 bytes                 -> '00'
      *>   N = 24  - 5 bytes (3 whole bytes of bits; more than the table's 20, still no more than 5 BYTES) -> '00'
      *>   N = 25  - 6 bytes                 -> '34'
      *> then the permanent error REMAINS IN EFFECT (§9.1.13.1) - the fourth READ, whose own count (the legal 20) would
      *> succeed, answers '34' again - and CLOSE ends it (Annex A.1 item 105, DOC-A.1-105), so the reopened READ of the
      *> first record is '00' again.
      *> WHY EACH LEG CAN FAIL: a comparison in BITS against the table's 20 would refuse N = 24 ('34'); one that
      *> rounded the last item's partial byte DOWN (25 bits as 3 bytes) would answer '00' for N = 25; one that skipped
      *> the rule would answer '00' at the third READ.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1880BT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT BT ASSIGN TO "pb1880bt.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT BTW ASSIGN TO "pb1880bt.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  BT.
       01  BT-R.
           05 BT-N PIC 99.
           05 BT-T PIC 1 USAGE BIT OCCURS 1 TO 20 TIMES DEPENDING ON BT-N.
       FD  BTW.
       01  BTW-R PIC X(5).
       WORKING-STORAGE SECTION.
       01  FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT BTW.
           MOVE "20ABC" TO BTW-R.
           WRITE BTW-R.
           MOVE "24ABC" TO BTW-R.
           WRITE BTW-R.
           MOVE "25ABC" TO BTW-R.
           WRITE BTW-R.
           MOVE "20ABC" TO BTW-R.
           WRITE BTW-R.
           CLOSE BTW.
           OPEN INPUT BT.
           READ BT NOT AT END DISPLAY "BT1 NAE N=" BT-N END-READ.
           DISPLAY "BT1 " FS.
           READ BT NOT AT END DISPLAY "BT2 NAE N=" BT-N END-READ.
           DISPLAY "BT2 " FS.
           READ BT NOT AT END DISPLAY "BT3 NAE N=" BT-N END-READ.
           DISPLAY "BT3 " FS.
           READ BT NOT AT END DISPLAY "BT4 NAE N=" BT-N END-READ.
           DISPLAY "BT4 " FS.
           CLOSE BT.
           DISPLAY "CLOSE " FS.
           OPEN INPUT BT.
           READ BT NOT AT END DISPLAY "BT5 NAE N=" BT-N END-READ.
           DISPLAY "BT5 " FS.
           CLOSE BT.
           STOP RUN.
