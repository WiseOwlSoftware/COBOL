      *>--- conformance-case
      *> id: g85_bf07_08
      *> rules: [1985-GR-VIII.4.6.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.6.4 GR4 (Relative I-O REWRITE) - "The logical
      *> record released by a successful execution of the REWRITE
      *> statement is no longer available in the record area unless the
      *> file-name associated with record-name-1 is specified in a SAME
      *> RECORD AREA clause. The logical record is also available to
      *> the program as a record of other files referenced in the same
      *> SAME RECORD AREA clause as the associated I-O file, as well as
      *> the file associated with record-name-1."
      *> RA (relative, random access, open I-O) and RB (relative) share a
      *> SAME RECORD AREA. RA's record 2 holds OLDVAL02; with RK-A = 2,
      *> REWRITE RA-REC after MOVE "NEWVAL02" (random access needs no
      *> prior READ).
      *> ST  - the REWRITE succeeds: status 00.
      *> RWA - RA-REC still holds NEWVAL02.
      *> RWB - RB's record (two 4-character fields) holds NEWV/AL02.
      *> FIL - RA's record 2 read back: NEWVAL02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0708.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RA ASSIGN TO "BF0708RA"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK-A
               FILE STATUS IS ST-A.
           SELECT RB ASSIGN TO "BF0708RB"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK-B.
       I-O-CONTROL.
           SAME RECORD AREA FOR RA RB.
       DATA DIVISION.
       FILE SECTION.
       FD  RA.
       01  RA-REC PIC X(8).
       FD  RB.
       01  RB-REC.
           05  RB-1 PIC X(4).
           05  RB-2 PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK-A PIC 9.
       01  RK-B PIC 9.
       01  ST-A PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT RA.
           MOVE 2 TO RK-A.
           MOVE "OLDVAL02" TO RA-REC.
           WRITE RA-REC INVALID KEY DISPLAY "W INVALID".
           CLOSE RA.
           OPEN I-O RA.
           OPEN OUTPUT RB.
           MOVE 2 TO RK-A.
           MOVE "NEWVAL02" TO RA-REC.
           REWRITE RA-REC INVALID KEY DISPLAY "RW INVALID".
           DISPLAY "ST=" ST-A.
           DISPLAY "RWA=" RA-REC.
           DISPLAY "RWB=" RB-1 "/" RB-2.
           CLOSE RA RB.
           OPEN INPUT RA.
           MOVE 2 TO RK-A.
           READ RA INVALID KEY DISPLAY "R INVALID".
           DISPLAY "FIL=" RA-REC.
           CLOSE RA.
           STOP RUN.
