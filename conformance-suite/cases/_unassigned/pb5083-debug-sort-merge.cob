      *>--- conformance-case
      *> id: pb5083-debug-sort-merge
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5083. X3.23-1985 USE FOR DEBUGGING general rule 19: "If
      *> reference to procedure-name-1 in the INPUT or OUTPUT phrase of a
      *> SORT or MERGE statement causes the debugging section to be
      *> executed": DEBUG-NAME contains procedure-name-1 and DEBUG-CONTENTS
      *> 'SORT INPUT' (the INPUT phrase of a SORT), 'SORT OUTPUT' (its
      *> OUTPUT phrase) or 'MERGE OUTPUT' (the OUTPUT phrase of a MERGE).
      *> Nothing in the debug module forbids an input or output procedure
      *> as the subject, so the program is legal.
      *> Expected output, in order: the SORT INPUT trigger, the SORT OUTPUT
      *> trigger and the one sorted key A, the MERGE OUTPUT trigger and the
      *> merged keys A and B (each trigger as the statement enters the
      *> procedure, before its first statement), then DONE. Why it can fail: a
      *> compiler that refuses the subject (the pre-fix COBOL1571) prints
      *> nothing, one that does not set the cause prints the cause of the
      *> previous transfer.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5083SMRG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ WITH DEBUGGING MODE.
       OBJECT-COMPUTER. XYZ.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SORT-FILE ASSIGN TO "pb5083-sort.wrk".
           SELECT MERGE-FILE ASSIGN TO "pb5083-merge.wrk".
           SELECT IN-A ASSIGN TO "pb5083-a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT IN-B ASSIGN TO "pb5083-b.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       SD  SORT-FILE.
       01  SR-REC.
           05 SR-KEY PIC X.
       SD  MERGE-FILE.
       01  MR-REC.
           05 MR-KEY PIC X.
       FD  IN-A.
       01  A-REC PIC X.
       FD  IN-B.
       01  B-REC PIC X.
       WORKING-STORAGE SECTION.
       01  W-NAME PIC X(8).
       01  W-CONT PIC X(13).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON SRT-IN SRT-OUT MRG-OUT.
       DBG-1.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "DEBUG [" W-NAME "] [" W-CONT "]".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-1.
           SORT SORT-FILE ON ASCENDING KEY SR-KEY
               INPUT PROCEDURE IS SRT-IN
               OUTPUT PROCEDURE IS SRT-OUT.
           OPEN OUTPUT IN-A.
           MOVE "A" TO A-REC.
           WRITE A-REC.
           CLOSE IN-A.
           OPEN OUTPUT IN-B.
           MOVE "B" TO B-REC.
           WRITE B-REC.
           CLOSE IN-B.
           MERGE MERGE-FILE ON ASCENDING KEY MR-KEY
               USING IN-A IN-B
               OUTPUT PROCEDURE IS MRG-OUT.
           DISPLAY "DONE".
           STOP RUN.
       SRT-IN SECTION.
       SRT-IN-1.
           MOVE "A" TO SR-KEY.
           RELEASE SR-REC.
       SRT-OUT SECTION.
       SRT-OUT-1.
           RETURN SORT-FILE AT END DISPLAY "EMPTY"
               NOT AT END DISPLAY SR-KEY.
       MRG-OUT SECTION.
       MRG-OUT-1.
           RETURN MERGE-FILE AT END DISPLAY "EMPTY"
               NOT AT END DISPLAY MR-KEY.
           RETURN MERGE-FILE AT END DISPLAY "EMPTY"
               NOT AT END DISPLAY MR-KEY.
