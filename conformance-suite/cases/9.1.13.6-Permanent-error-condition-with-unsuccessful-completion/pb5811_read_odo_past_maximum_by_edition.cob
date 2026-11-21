      *>--- conformance-case
      *> id: pb5811_read_odo_past_maximum_by_edition
      *> rules: [GR-9.1.13.6-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      * §2023:9.1.13.6 GR4b is a 2023 condition: I-O status 34 for a READ
      * of a record whose OCCURS DEPENDING ON item makes it longer than
      * the file description's maximum. The 1985, 2002 and 2014 status
      * lists have no such condition, so a READ there is successful: 00
      * and no permanent error (§1985:IX.4.5.4 GR18 - a record longer
      * than the file's maximum is still read; the READ is successful).
      * The record "11ABCDEFGHIJ" is 12 bytes, the FD's maximum; its
      * count 11 would make the 2-byte count plus 11 = 13 bytes.
      * The second READ, a legal count 03, shows the permanent error is
      * in effect after the 2023 34 only (the .2023.out).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5811ED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OD ASSIGN TO "pb5811s.dat"
               ORGANIZATION IS SEQUENTIAL FILE STATUS IS FS.
           SELECT ODW ASSIGN TO "pb5811s.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT ODR ASSIGN TO "pb5811r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKR FILE STATUS IS FS.
           SELECT ODRW ASSIGN TO "pb5811r.dat"
               ORGANIZATION IS RELATIVE ACCESS MODE IS RANDOM
               RELATIVE KEY IS RKW.
       DATA DIVISION.
       FILE SECTION.
       FD  OD.
       01  OD-R.
           05 OD-N PIC 99.
           05 OD-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON OD-N.
       FD  ODW.
       01  ODW-R PIC X(12).
       FD  ODR.
       01  ODR-R.
           05 ODR-N PIC 99.
           05 ODR-T PIC X OCCURS 1 TO 10 TIMES DEPENDING ON ODR-N.
       FD  ODRW.
       01  ODRW-R PIC X(12).
       WORKING-STORAGE SECTION.
       01  FS  PIC XX.
       01  RKR PIC 9.
       01  RKW PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT ODW.
           MOVE "11ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           MOVE "03ABCDEFGHIJ" TO ODW-R.
           WRITE ODW-R.
           CLOSE ODW.
           OPEN INPUT OD.
           READ OD AT END CONTINUE.
           DISPLAY "SEQ1 " FS.
           READ OD AT END CONTINUE.
           DISPLAY "SEQ2 " FS.
           CLOSE OD.
           OPEN OUTPUT ODRW.
           MOVE 1 TO RKW.
           MOVE "11ABCDEFGHIJ" TO ODRW-R.
           WRITE ODRW-R INVALID KEY CONTINUE.
           MOVE 2 TO RKW.
           MOVE "03ABCDEFGHIJ" TO ODRW-R.
           WRITE ODRW-R INVALID KEY CONTINUE.
           CLOSE ODRW.
           OPEN INPUT ODR.
           MOVE 1 TO RKR.
           READ ODR INVALID KEY CONTINUE.
           DISPLAY "REL1 " FS.
           MOVE 2 TO RKR.
           READ ODR INVALID KEY CONTINUE.
           DISPLAY "REL2 " FS.
           CLOSE ODR.
           STOP RUN.
