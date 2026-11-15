      *>--- conformance-case
      *> id: pb5387-external-record-varying-agree
      *> rules: [1985-GR-VII.3.8.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5387. §1985:VII.3.8.4 GR2: the entries of an external file
      *> connector "must specify the same values for integer-1 or integer-2
      *> and integer-3". MAIN writes RECORD IS VARYING IN SIZE FROM 2 TO 6 and
      *> the called program writes the same pair as RECORD CONTAINS 2 TO 6
      *> (Format 3, integer-4 and integer-5): the same values, so they agree
      *> and the program runs.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5387B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5387b.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           RECORD IS VARYING IN SIZE FROM 2 TO 6 CHARACTERS.
       01  SF-REC PIC X(6).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCDEF" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5387BS"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5387B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5387BS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5387b.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           RECORD CONTAINS 2 TO 6 CHARACTERS.
       01  SF-REC PIC X(6).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5387BS.
