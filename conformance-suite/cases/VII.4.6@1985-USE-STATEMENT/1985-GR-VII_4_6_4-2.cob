      *>--- conformance-case
      *> id: 1985-GR-VII_4_6_4-2
      *> rules: [1985-GR-VII.4.6.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.6.4 GR2: "Within a declarative procedure, there
      *> must be no reference to any nondeclarative procedures." The
      *> legal form: the USE procedure references only declarative
      *> procedures (a PERFORM of SQ-ERR-NOTE and a GO TO SQ-ERR-END).
      *> The second READ finds no next record (I-O status 10,
      *> §1985:VII.1.3.5) and has no AT END phrase, so the USE procedure
      *> runs (GR5), and control returns to the DISPLAY after the READ
      *> (GR6). WiseOwl warns (COBOL2375) on the violating form, a
      *> reference to a nondeclarative procedure (docs/CONFORMANCE.md
      *> D-DECLREF; kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DREFSQ.
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
           PERFORM SQ-ERR-NOTE.
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
