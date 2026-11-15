      *>--- conformance-case
      *> id: 1985-GR-IX_4_8_4-2
      *> rules: [1985-GR-IX.4.8.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.8.4 GR2: "Within a declarative procedure, there
      *> must be no reference to any nondeclarative procedures." The
      *> legal form: the USE procedure references only declarative
      *> procedures (a PERFORM of IX-ERR-NOTE and a GO TO IX-ERR-END).
      *> The second READ finds no next record (I-O status 10,
      *> §1985:IX.1.3.4) and has no AT END phrase, so the USE procedure
      *> runs (GR5), and control returns to the DISPLAY after the READ
      *> (GR6). WiseOwl warns (COBOL2375) on the violating form, a
      *> reference to a nondeclarative procedure (docs/CONFORMANCE.md
      *> D-DECLREF; kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DREFIX.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-FILE ASSIGN TO "drefix.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS SEQUENTIAL
               RECORD KEY IS IX-KEY
               FILE STATUS IS IX-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-FILE.
       01  IX-REC.
           05  IX-KEY  PIC X(4).
           05  IX-DATA PIC X(4).
       WORKING-STORAGE SECTION.
       01  IX-STATUS PIC XX VALUE SPACES.
       PROCEDURE DIVISION.
       DECLARATIVES.
       IX-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON IX-FILE.
       IX-ERR-1.
           DISPLAY "USE PROCEDURE STATUS " IX-STATUS.
           PERFORM IX-ERR-NOTE.
           GO TO IX-ERR-END.
       IX-ERR-NOTE.
           DISPLAY "DECLARATIVE PARAGRAPH PERFORMED".
       IX-ERR-SKIPPED.
           DISPLAY "GO TO DID NOT TRANSFER".
       IX-ERR-END.
           EXIT.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           OPEN OUTPUT IX-FILE.
           MOVE "K001" TO IX-KEY.
           MOVE "DATA" TO IX-DATA.
           WRITE IX-REC.
           CLOSE IX-FILE.
           OPEN INPUT IX-FILE.
           READ IX-FILE.
           DISPLAY "READ 1 STATUS " IX-STATUS " " IX-REC.
           READ IX-FILE.
           DISPLAY "READ 2 STATUS " IX-STATUS.
           CLOSE IX-FILE.
           STOP RUN.
