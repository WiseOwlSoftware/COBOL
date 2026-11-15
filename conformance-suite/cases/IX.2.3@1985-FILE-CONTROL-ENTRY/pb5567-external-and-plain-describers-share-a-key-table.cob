      *>--- conformance-case
      *> id: pb5567-external-and-plain-describers-share-a-key-table
      *> rules: [1985-GR-IX.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5567. The alternate keys of a physical indexed file are the file's, not the
      *> entry's: an entry that is EXTERNAL and one that is not describe one file when they
      *> list the same alternate keys with the same DUPLICATES phrases (§1985:IX.2.3.4 GR1), in
      *> either order (§1985:IX.2.3.3 SR1). The plain entry creates one file and the external
      *> entry, whose clauses are in the other order, reads it by each alternate key; the
      *> external entry creates a second and the plain entry reads it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5567BMAIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PLF ASSIGN TO "PB5567XDATA"
           ORGANIZATION IS INDEXED
           ACCESS MODE IS DYNAMIC
           RECORD KEY IS PLF-KEY
           ALTERNATE RECORD KEY IS PLF-ALT WITH DUPLICATES
           ALTERNATE RECORD KEY IS PLF-ALT2
           FILE STATUS IS PLF-ST.
           SELECT QLF ASSIGN TO "PB5567YDATA"
           ORGANIZATION IS INDEXED
           ACCESS MODE IS DYNAMIC
           RECORD KEY IS QLF-KEY
           ALTERNATE RECORD KEY IS QLF-ALT WITH DUPLICATES
           ALTERNATE RECORD KEY IS QLF-ALT2
           FILE STATUS IS QLF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  PLF.
       01  PLF-REC.
           05 PLF-KEY  PIC X(4).
           05 PLF-ALT  PIC X(2).
           05 PLF-ALT2 PIC X(2).
           05 PLF-DATA PIC X(5).
       FD  QLF.
       01  QLF-REC.
           05 QLF-KEY  PIC X(4).
           05 QLF-ALT  PIC X(2).
           05 QLF-ALT2 PIC X(2).
           05 QLF-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01  PLF-ST PIC XX.
       01  QLF-ST PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT PLF.
           DISPLAY "PLF OPEN " PLF-ST.
           MOVE "K001" TO PLF-KEY. MOVE "A1" TO PLF-ALT.
           MOVE "B2" TO PLF-ALT2. MOVE "ALPHA" TO PLF-DATA.
           WRITE PLF-REC INVALID KEY DISPLAY "WRITE FAILED".
           MOVE "K002" TO PLF-KEY. MOVE "A2" TO PLF-ALT.
           MOVE "B1" TO PLF-ALT2. MOVE "GAMMA" TO PLF-DATA.
           WRITE PLF-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE PLF.
           CALL "PB5567BSUB".
           OPEN INPUT QLF.
           DISPLAY "QLF OPEN " QLF-ST.
           MOVE "B1" TO QLF-ALT2.
           READ QLF KEY IS QLF-ALT2 INVALID KEY DISPLAY "RD ALT2 FAIL".
           DISPLAY QLF-DATA.
           MOVE "A1" TO QLF-ALT.
           READ QLF KEY IS QLF-ALT INVALID KEY DISPLAY "RD ALT FAIL".
           DISPLAY QLF-DATA.
           CLOSE QLF.
           DISPLAY "MAIN DONE".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5567BSUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT EF ASSIGN TO "PB5567XDATA"
           ORGANIZATION IS INDEXED
           ACCESS MODE IS DYNAMIC
           RECORD KEY IS EF-KEY
           ALTERNATE RECORD KEY IS EF-ALT2
           ALTERNATE RECORD KEY IS EF-ALT WITH DUPLICATES
           FILE STATUS IS EF-ST.
           SELECT GF ASSIGN TO "PB5567YDATA"
           ORGANIZATION IS INDEXED
           ACCESS MODE IS DYNAMIC
           RECORD KEY IS GF-KEY
           ALTERNATE RECORD KEY IS GF-ALT2
           ALTERNATE RECORD KEY IS GF-ALT WITH DUPLICATES
           FILE STATUS IS GF-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  EF IS EXTERNAL.
       01  EF-REC.
           05 EF-KEY  PIC X(4).
           05 EF-ALT  PIC X(2).
           05 EF-ALT2 PIC X(2).
           05 EF-DATA PIC X(5).
       FD  GF IS EXTERNAL.
       01  GF-REC.
           05 GF-KEY  PIC X(4).
           05 GF-ALT  PIC X(2).
           05 GF-ALT2 PIC X(2).
           05 GF-DATA PIC X(5).
       WORKING-STORAGE SECTION.
       01  EF-ST PIC XX.
       01  GF-ST PIC XX.
       PROCEDURE DIVISION.
       SUB-PARA.
           OPEN INPUT EF.
           DISPLAY "EF OPEN " EF-ST.
           MOVE "B1" TO EF-ALT2.
           READ EF KEY IS EF-ALT2 INVALID KEY DISPLAY "RD ALT2 FAIL".
           DISPLAY EF-DATA.
           MOVE "A1" TO EF-ALT.
           READ EF KEY IS EF-ALT INVALID KEY DISPLAY "RD ALT FAIL".
           DISPLAY EF-DATA.
           CLOSE EF.
           OPEN OUTPUT GF.
           DISPLAY "GF OPEN " GF-ST.
           MOVE "K001" TO GF-KEY. MOVE "A1" TO GF-ALT.
           MOVE "B2" TO GF-ALT2. MOVE "DELTA" TO GF-DATA.
           WRITE GF-REC INVALID KEY DISPLAY "WRITE FAILED".
           MOVE "K002" TO GF-KEY. MOVE "A2" TO GF-ALT.
           MOVE "B1" TO GF-ALT2. MOVE "OMEGA" TO GF-DATA.
           WRITE GF-REC INVALID KEY DISPLAY "WRITE FAILED".
           CLOSE GF.
           EXIT PROGRAM.
       END PROGRAM PB5567BSUB.
       END PROGRAM PB5567BMAIN.
