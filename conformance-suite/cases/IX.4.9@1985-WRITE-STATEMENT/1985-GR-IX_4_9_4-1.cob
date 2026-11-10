      *>--- conformance-case
      *> id: 1985-GR-IX_4_9_4-1
      *> rules: [1985-GR-IX.4.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.9.4 GR1 (THE WRITE STATEMENT). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the confirmation pass's redraft (WEAK original) by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. IXWRT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DYN-FILE ASSIGN TO IXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS D-KEY
               FILE STATUS IS D-STAT.
           SELECT SEQ-FILE ASSIGN TO IXFILE
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS S-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  DYN-FILE
           LABEL RECORDS ARE STANDARD.
       01  D-REC.
           05  D-KEY  PIC X(4).
           05  D-DATA PIC X(10).
       FD  SEQ-FILE
           LABEL RECORDS ARE STANDARD.
       01  S-REC.
           05  S-KEY  PIC X(4).
           05  S-DATA PIC X(10).
       WORKING-STORAGE SECTION.
       01  D-STAT PIC XX.
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-SEC SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON DYN-FILE.
       ERR-PARA.
           DISPLAY "USE " D-STAT.
       END DECLARATIVES.
       MAIN-SEC SECTION.
       MAIN-PARA.
           OPEN OUTPUT DYN-FILE.
           MOVE "0002" TO D-KEY.
           MOVE "OUTPUT" TO D-DATA.
           WRITE D-REC
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "OUTPUT OK"
           END-WRITE.
           CLOSE DYN-FILE.
           OPEN I-O DYN-FILE.
           MOVE "0001" TO D-KEY.
           MOVE "I-O" TO D-DATA.
           WRITE D-REC
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "I-O OK"
           END-WRITE.
           CLOSE DYN-FILE.
           OPEN EXTEND SEQ-FILE.
           MOVE "0003" TO S-KEY.
           MOVE "EXTEND" TO S-DATA.
           WRITE S-REC
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "EXTEND OK"
           END-WRITE.
           CLOSE SEQ-FILE.
           OPEN INPUT DYN-FILE.
           MOVE "0004" TO D-KEY.
           MOVE "INPUT" TO D-DATA.
           WRITE D-REC
               INVALID KEY DISPLAY "INVALID"
               NOT INVALID KEY DISPLAY "WRITTEN"
           END-WRITE.
           STOP RUN.
