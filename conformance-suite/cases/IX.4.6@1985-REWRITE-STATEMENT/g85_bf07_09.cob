      *>--- conformance-case
      *> id: g85_bf07_09
      *> rules: [1985-GR-IX.4.6.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.6.4 GR4 (Indexed I-O REWRITE) - "The logical record
      *> released by a successful execution of the REWRITE statement is
      *> no longer available in the record area unless the file-name
      *> associated with record-name-1 is specified in a SAME RECORD
      *> AREA clause. The logical record is available to the program as
      *> a record of other files referenced in the SAME RECORD AREA
      *> clause as the associated I-O file, as well as the file
      *> associated with record-name-1."
      *> XA (indexed, random access, open I-O, prime key the first 3
      *> characters) and SB (sequential) share a SAME RECORD AREA (the
      *> files need not have one organization: §1985:VII.2.13.3 SR4).
      *> XA holds K01OLD01; REWRITE XA-REC FROM WS-NEW (K01NEW01, the
      *> same key).
      *> ST  - the REWRITE succeeds: status 00.
      *> RWA - XA-REC still holds K01NEW01.
      *> RWB - SB's record (fields of 3 + 5) holds K01/NEW01.
      *> FIL - XA read back by key K01: K01NEW01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0709.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XA ASSIGN TO "BF0709XA"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS XA-KEY
               FILE STATUS IS ST-A.
           SELECT SB ASSIGN TO "BF0709SB".
       I-O-CONTROL.
           SAME RECORD AREA FOR XA SB.
       DATA DIVISION.
       FILE SECTION.
       FD  XA.
       01  XA-REC.
           05  XA-KEY PIC X(3).
           05  XA-DATA PIC X(5).
       FD  SB.
       01  SB-REC.
           05  SB-1 PIC X(3).
           05  SB-2 PIC X(5).
       WORKING-STORAGE SECTION.
       01  ST-A PIC XX.
       01  WS-NEW PIC X(8) VALUE "K01NEW01".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT XA.
           MOVE "K01OLD01" TO XA-REC.
           WRITE XA-REC INVALID KEY DISPLAY "W INVALID".
           CLOSE XA.
           OPEN I-O XA.
           OPEN OUTPUT SB.
           REWRITE XA-REC FROM WS-NEW
               INVALID KEY DISPLAY "RW INVALID".
           DISPLAY "ST=" ST-A.
           DISPLAY "RWA=" XA-REC.
           DISPLAY "RWB=" SB-1 "/" SB-2.
           CLOSE XA SB.
           OPEN INPUT XA.
           MOVE "K01" TO XA-KEY.
           READ XA INVALID KEY DISPLAY "R INVALID".
           DISPLAY "FIL=" XA-REC.
           CLOSE XA.
           STOP RUN.
