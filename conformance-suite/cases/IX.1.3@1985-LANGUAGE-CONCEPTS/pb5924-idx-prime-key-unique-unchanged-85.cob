      *>--- conformance-case
      *> id: pb5924-idx-prime-key-unique-unchanged-85
      *> rules: [1985-GR-IX.1.3.1-p2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IX.1.3.1 (cite.py: OK): "The prime record key of each record in the file must be unique"
      *> A duplicate prime key WRITE is an invalid key (22); a REWRITE whose prime
      *> key changed since the READ, sequential access, is invalid (21); file unchanged.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IDXPRI2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IFILE ASSIGN TO "IDXPRI2.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IKEY
               FILE STATUS IS WS-STAT.
           SELECT RFILE ASSIGN TO "IDXPRI2.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS RKEY
               FILE STATUS IS WS-STAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IFILE
           LABEL RECORDS ARE STANDARD.
       01  IREC.
           05  IKEY  PIC X(3).
           05  IDATA PIC X(5).
       FD  RFILE
           LABEL RECORDS ARE STANDARD.
       01  RREC.
           05  RKEY  PIC X(3).
           05  RDATA PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-STAT PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT IFILE.
           MOVE "K01" TO IKEY. MOVE "ONE  " TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "INVALID K01" END-WRITE.
           MOVE "K02" TO IKEY. MOVE "TWO  " TO IDATA.
           WRITE IREC INVALID KEY DISPLAY "INVALID K02" END-WRITE.
           CLOSE IFILE.
           OPEN I-O RFILE.
           MOVE "K01" TO RKEY. MOVE "DUP  " TO RDATA.
           WRITE RREC
               INVALID KEY DISPLAY "DUPLICATE PRIME KEY " WS-STAT
               NOT INVALID KEY DISPLAY "WRITTEN " WS-STAT
           END-WRITE.
           CLOSE RFILE.
           OPEN I-O IFILE.
           READ IFILE AT END DISPLAY "EOF" END-READ.
           MOVE "K09" TO IKEY. MOVE "NINE " TO IDATA.
           REWRITE IREC
               INVALID KEY DISPLAY "KEY CHANGED " WS-STAT
               NOT INVALID KEY DISPLAY "REWRITTEN " WS-STAT
           END-REWRITE.
           CLOSE IFILE.
           OPEN INPUT IFILE.
           READ IFILE AT END DISPLAY "EOF" END-READ.
           DISPLAY "REC " IKEY " " IDATA.
           READ IFILE AT END DISPLAY "EOF" END-READ.
           DISPLAY "REC " IKEY " " IDATA.
           READ IFILE AT END DISPLAY "EOF " WS-STAT END-READ.
           CLOSE IFILE.
           STOP RUN.
