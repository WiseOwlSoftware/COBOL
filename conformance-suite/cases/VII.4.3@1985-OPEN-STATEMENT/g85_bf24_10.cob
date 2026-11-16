      *>--- conformance-case
      *> id: g85_bf24_10
      *> rules: [1985-GR-VII.4.3.4-14]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR14 - "When files are opened with the INPUT
      *> or I-O phrase, the file position indicator is set to 1." The
      *> next sequential READ then makes record 1 available
      *> (§1985:VII.4.4.4 GR4). The file holds SEQ001 SEQ002 SEQ003.
      *>   OPEN INPUT, two READs: Q1=SEQ001, Q2=SEQ002.
      *>   CLOSE, OPEN INPUT again: Q3=SEQ001 - the OPEN reset the
      *>   indicator, it did not resume at record 3.
      *>   CLOSE, OPEN I-O: Q4=SEQ001 - the I-O phrase also sets 1.
      *>   Two READs, CLOSE, OPEN I-O again: Q5=SEQ001.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2410.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FQ ASSIGN TO "g85bf2410.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FQ.
       01  Q-REC PIC X(6).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FQ.
           MOVE "SEQ001" TO Q-REC.
           WRITE Q-REC.
           MOVE "SEQ002" TO Q-REC.
           WRITE Q-REC.
           MOVE "SEQ003" TO Q-REC.
           WRITE Q-REC.
           CLOSE FQ.
           OPEN INPUT FQ.
           READ FQ AT END DISPLAY "EOF-EARLY".
           DISPLAY "Q1=" Q-REC.
           READ FQ AT END DISPLAY "EOF-EARLY".
           DISPLAY "Q2=" Q-REC.
           CLOSE FQ.
           OPEN INPUT FQ.
           READ FQ AT END DISPLAY "EOF-EARLY".
           DISPLAY "Q3=" Q-REC.
           CLOSE FQ.
           OPEN I-O FQ.
           READ FQ AT END DISPLAY "EOF-EARLY".
           DISPLAY "Q4=" Q-REC.
           READ FQ AT END DISPLAY "EOF-EARLY".
           READ FQ AT END DISPLAY "EOF-EARLY".
           CLOSE FQ.
           OPEN I-O FQ.
           READ FQ AT END DISPLAY "EOF-EARLY".
           DISPLAY "Q5=" Q-REC.
           CLOSE FQ.
           STOP RUN.
