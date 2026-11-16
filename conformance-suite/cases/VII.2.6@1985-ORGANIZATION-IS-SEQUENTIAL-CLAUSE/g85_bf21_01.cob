      *>--- conformance-case
      *> id: g85_bf21_01
      *> rules: [1985-GR-VII.2.6.3-2, 1985-GR-VII.2.6.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.6.3 GR2 - a sequential record is identified by the
      *> predecessor-successor relationship set when it is placed in the
      *> file: three records written in the non-collating order CHARLIE,
      *> ALPHA, BRAVO, the file closed and reopened, read back in that
      *> same order and then AT END (the "E" lines). A file that sorted
      *> or keyed its records would read ALPHA first and fail.
      *> §1985:VII.2.6.3 GR3 - SEQ-I has no ORGANIZATION clause, so is
      *> sequential: the same writes and reads give the same order (the
      *> "I" lines), and its WRITE carries no INVALID KEY phrase, which
      *> only a sequential file allows. An implied organization other
      *> than sequential refuses the WRITE or changes the "I" lines.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2101.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-E ASSIGN TO "BF2101E.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SEQ-I ASSIGN TO "BF2101I.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-E.
       01  E-REC                PIC X(8).
       FD  SEQ-I.
       01  I-REC                PIC X(8).
       WORKING-STORAGE SECTION.
       01  WS-CNT               PIC 9 VALUE 0.
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT SEQ-E SEQ-I.
           MOVE "CHARLIE" TO E-REC I-REC.
           WRITE E-REC.
           WRITE I-REC.
           MOVE "ALPHA" TO E-REC I-REC.
           WRITE E-REC.
           WRITE I-REC.
           MOVE "BRAVO" TO E-REC I-REC.
           WRITE E-REC.
           WRITE I-REC.
           CLOSE SEQ-E SEQ-I.
           OPEN INPUT SEQ-E.
           MOVE 0 TO WS-CNT.
           MOVE "N" TO WS-EOF.
           PERFORM READ-E UNTIL WS-EOF = "Y".
           CLOSE SEQ-E.
           OPEN INPUT SEQ-I.
           MOVE 0 TO WS-CNT.
           MOVE "N" TO WS-EOF.
           PERFORM READ-I UNTIL WS-EOF = "Y".
           CLOSE SEQ-I.
           STOP RUN.
       READ-E.
           READ SEQ-E
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "E END AFTER " WS-CNT
               NOT AT END
                   ADD 1 TO WS-CNT
                   DISPLAY "E " WS-CNT " [" E-REC "]"
           END-READ.
       READ-I.
           READ SEQ-I
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "I END AFTER " WS-CNT
               NOT AT END
                   ADD 1 TO WS-CNT
                   DISPLAY "I " WS-CNT " [" I-REC "]"
           END-READ.
