      *>--- conformance-case
      *> id: pb5567-external-alternate-keys-duplicates-differ
      *> rules: [1985-GR-IX.2.3.4-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5567. The second entry gives IXF-ALT2 a DUPLICATES phrase the first lacks:
      *> not the same DUPLICATES phrase (§1985:IX.2.3.4 GR1), COBOL6847.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5567BMAIN.
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
           CALL "PB5567BSUB".
           CLOSE IXF.
           DISPLAY "MAIN DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5567BSUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "PB5567DATA"
           ORGANIZATION IS INDEXED
           ACCESS MODE IS DYNAMIC
           RECORD KEY IS IXF-KEY
           ALTERNATE RECORD KEY IS IXF-ALT2 WITH DUPLICATES
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
       END PROGRAM PB5567BSUB.
       END PROGRAM PB5567BMAIN.
