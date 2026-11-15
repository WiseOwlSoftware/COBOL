      *>--- conformance-case
      *> id: pb5567-external-alternate-keys-in-another-order
      *> rules: [1985-GR-IX.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5567. Two entries of one external file that name the same alternate keys
      *> with the same DUPLICATES phrases agree (§1985:IX.2.3.4 GR1: "the same number of
      *> alternate record keys, and the same DUPLICATES phrase") whatever order
      *> the clauses are written in (§1985:IX.2.3.3 SR1: any order).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5567AMAIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "PB5567DATA"
           ORGANIZATION IS INDEXED
           ACCESS MODE IS DYNAMIC
           RECORD KEY IS IXF-KEY
           ALTERNATE RECORD KEY IS IXF-ALT WITH DUPLICATES
           ALTERNATE RECORD KEY IS IXF-ALT2.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF IS EXTERNAL.
       01  IXF-REC.
           05 IXF-KEY  PIC X(4).
           05 IXF-ALT  PIC X(2).
           05 IXF-ALT2 PIC X(2).
           05 IXF-DATA PIC X(5).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IXF.
           MOVE "K001" TO IXF-KEY. MOVE "A1" TO IXF-ALT.
           MOVE "B1" TO IXF-ALT2. MOVE "ALPHA" TO IXF-DATA.
           WRITE IXF-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE IXF.
           OPEN INPUT IXF.
           CALL "PB5567ASUB".
           CLOSE IXF.
           DISPLAY "MAIN DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5567ASUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "PB5567DATA"
           ORGANIZATION IS INDEXED
           ACCESS MODE IS DYNAMIC
           RECORD KEY IS IXF-KEY
           ALTERNATE RECORD KEY IS IXF-ALT2
           ALTERNATE RECORD KEY IS IXF-ALT WITH DUPLICATES.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF IS EXTERNAL.
       01  IXF-REC.
           05 IXF-KEY  PIC X(4).
           05 IXF-ALT  PIC X(2).
           05 IXF-ALT2 PIC X(2).
           05 IXF-DATA PIC X(5).
       PROCEDURE DIVISION.
       SUB-PARA.
           MOVE "K001" TO IXF-KEY.
           READ IXF RECORD INVALID KEY DISPLAY "READ FAILED".
           DISPLAY IXF-DATA.
           EXIT PROGRAM.
       END PROGRAM PB5567ASUB.
       END PROGRAM PB5567AMAIN.
