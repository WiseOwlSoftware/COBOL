      *>--- conformance-case
      *> id: 1985-GR-VIII_4_8_4-2
      *> rules: [1985-GR-VIII.4.8.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.8.4 GR2: "Within a declarative procedure, there
      *> must be no reference to any nondeclarative procedures." The
      *> legal form: the USE procedure references only declarative
      *> procedures (a PERFORM of RL-ERR-NOTE and a GO TO RL-ERR-END).
      *> The second READ finds no next record (I-O status 10,
      *> §1985:VIII.1.3.4) and has no AT END phrase, so the USE
      *> procedure runs (GR5), and control returns to the DISPLAY after
      *> the READ (GR6). WiseOwl warns (COBOL2375) on the violating
      *> form, a reference to a nondeclarative procedure
      *> (docs/CONFORMANCE.md D-DECLREF; kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DREFRL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RL-FILE ASSIGN TO "drefrl.dat"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS RL-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  RL-FILE.
       01  RL-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  RL-STATUS PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       RL-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RL-FILE.
       RL-ERR-1.
           DISPLAY "USE PROCEDURE STATUS " RL-STATUS.
           PERFORM RL-ERR-NOTE.
           GO TO RL-ERR-END.
       RL-ERR-NOTE.
           DISPLAY "DECLARATIVE PARAGRAPH PERFORMED".
       RL-ERR-SKIPPED.
           DISPLAY "GO TO DID NOT TRANSFER".
       RL-ERR-END.
           EXIT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT RL-FILE.
           MOVE "REC-0001" TO RL-REC.
           WRITE RL-REC.
           CLOSE RL-FILE.
           OPEN INPUT RL-FILE.
           READ RL-FILE.
           DISPLAY "READ 1 STATUS " RL-STATUS " " RL-REC.
           READ RL-FILE.
           DISPLAY "READ 2 STATUS " RL-STATUS.
           CLOSE RL-FILE.
           STOP RUN.
