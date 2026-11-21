      *>--- conformance-case
      *> id: 1985-GR-VIII_4_8_4-2-nondeclarative-reference
      *> rules: [1985-GR-VIII.4.8.4-2]
      *> flag: {kind: syntax, at: [85], why: "the reference to a nondeclarative procedure is found from the names of the program alone"}
      *> control: 1985-GR-VIII_4_8_4-2
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VIII.4.8.4 GR2: "Within a declarative procedure, there must be no
      *> reference to any nondeclarative procedures." The line whose
      *> sequence number area reads << PERFORMs P-1, a procedure of the
      *> nondeclarative part: the violation. Its control 1985-GR-VIII_4_8_4-2
      *> performs a declarative paragraph there. WiseOwl compiles it and
      *> warns (COBOL2375, docs/CONFORMANCE.md D-DECLREF; kb/Work PB4995,
      *> PB5461).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DREFRLF.
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
<<         PERFORM P-1.
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
