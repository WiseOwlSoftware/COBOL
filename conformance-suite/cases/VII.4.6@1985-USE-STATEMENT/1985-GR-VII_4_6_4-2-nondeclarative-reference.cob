      *>--- conformance-case
      *> id: 1985-GR-VII_4_6_4-2-nondeclarative-reference
      *> rules: [1985-GR-VII.4.6.4-2]
      *> flag: {kind: syntax, at: [85], why: "the reference to a nondeclarative procedure is found from the names of the program alone"}
      *> control: 1985-GR-VII_4_6_4-2
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.6.4 GR2: "Within a declarative procedure, there must be no
      *> reference to any nondeclarative procedures." The line whose
      *> sequence number area reads << PERFORMs P-1, a procedure of the
      *> nondeclarative part: the violation. Its control 1985-GR-VII_4_6_4-2
      *> performs a declarative paragraph there. WiseOwl compiles it and
      *> warns (COBOL2375, docs/CONFORMANCE.md D-DECLREF; kb/Work PB4995,
      *> PB5461).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DREFSQF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SQ-FILE ASSIGN TO "drefsq.dat"
               ORGANIZATION IS SEQUENTIAL
               ACCESS MODE IS SEQUENTIAL
               FILE STATUS IS SQ-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  SQ-FILE.
       01  SQ-REC PIC X(8).
       WORKING-STORAGE SECTION.
       01  SQ-STATUS PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       SQ-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON SQ-FILE.
       SQ-ERR-1.
           DISPLAY "USE PROCEDURE STATUS " SQ-STATUS.
<<         PERFORM P-1.
           GO TO SQ-ERR-END.
       SQ-ERR-NOTE.
           DISPLAY "DECLARATIVE PARAGRAPH PERFORMED".
       SQ-ERR-SKIPPED.
           DISPLAY "GO TO DID NOT TRANSFER".
       SQ-ERR-END.
           EXIT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT SQ-FILE.
           MOVE "REC-0001" TO SQ-REC.
           WRITE SQ-REC.
           CLOSE SQ-FILE.
           OPEN INPUT SQ-FILE.
           READ SQ-FILE.
           DISPLAY "READ 1 STATUS " SQ-STATUS " " SQ-REC.
           READ SQ-FILE.
           DISPLAY "READ 2 STATUS " SQ-STATUS.
           CLOSE SQ-FILE.
           STOP RUN.
