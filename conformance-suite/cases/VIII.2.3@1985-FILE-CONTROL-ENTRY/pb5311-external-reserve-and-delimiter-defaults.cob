      *>--- conformance-case
      *> id: pb5311-external-reserve-and-delimiter-defaults
      *> rules: [1985-GR-VIII.2.3.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5311. The entries of an external file connector agree
      *> when they specify "The same value for integer-1 in the RESERVE
      *> clause" (§1985:VIII.2.3.4 GR1), and a clause the entry omits has
      *> the implementor's value (§1985:VII.2.9.3 GR1: "the number of
      *> input-output areas allocated is specified by the implementor";
      *> the implementor's is one area, DOC-A.1-164), so an entry with no
      *> RESERVE clause and one that writes RESERVE 1 AREA specify the
      *> same connector, and so do an omitted RECORD DELIMITER clause and
      *> STANDARD-1 (§1985:VII.2.8.4 GR4, the implementor's one method).
      *> MAIN writes the relative file with neither clause; SUBPGM reads
      *> it with both written. Both compile and run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5311MAIN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL REL-FILE ASSIGN TO "PB5311XF"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS REL-KEY.
           SELECT SEQ-FILE ASSIGN TO "PB5311XS".
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(4).
       FD  SEQ-FILE IS EXTERNAL.
       01  SEQ-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT REL-FILE
           MOVE "WXYZ" TO REL-REC
           WRITE REL-REC INVALID KEY DISPLAY "WRITE FAILED" END-WRITE
           CLOSE REL-FILE
           OPEN OUTPUT SEQ-FILE
           MOVE "ABCD" TO SEQ-REC
           WRITE SEQ-REC
           CLOSE SEQ-FILE
           OPEN INPUT REL-FILE
           OPEN INPUT SEQ-FILE
           CALL "PB5311SUB"
           CLOSE REL-FILE
           CLOSE SEQ-FILE
           DISPLAY "MAIN DONE"
           STOP RUN.
       END PROGRAM PB5311MAIN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5311SUB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL REL-FILE ASSIGN TO "PB5311XF"
               RESERVE 1 AREA
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               RELATIVE KEY IS REL-KEY.
           SELECT SEQ-FILE ASSIGN TO "PB5311XS"
               RECORD DELIMITER IS STANDARD-1.
       DATA DIVISION.
       FILE SECTION.
       FD  REL-FILE IS EXTERNAL.
       01  REL-REC PIC X(4).
       FD  SEQ-FILE IS EXTERNAL.
       01  SEQ-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  REL-KEY PIC 9(4) EXTERNAL.
       PROCEDURE DIVISION.
       P-SUB.
           READ REL-FILE AT END DISPLAY "SUB EOF"
               NOT AT END DISPLAY "SUB READ " REL-REC
           END-READ
           READ SEQ-FILE AT END DISPLAY "SUB EOF"
               NOT AT END DISPLAY "SUB READ " SEQ-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM PB5311SUB.
