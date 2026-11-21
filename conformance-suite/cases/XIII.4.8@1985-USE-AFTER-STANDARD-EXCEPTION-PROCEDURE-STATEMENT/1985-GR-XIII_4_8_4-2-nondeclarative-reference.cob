      *>--- conformance-case
      *> id: 1985-GR-XIII_4_8_4-2-nondeclarative-reference
      *> rules: [1985-GR-XIII.4.8.4-2]
      *> flag: {kind: syntax, at: [85], why: "the reference to a nondeclarative procedure is found from the names of the program alone"}
      *> control: 1985-GR-XIII_4_8_4-2
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.4 GR2: "Within a declarative procedure, there must be no
      *> reference to any nondeclarative procedures." The line whose
      *> sequence number area reads << PERFORMs P-1, a procedure of the
      *> nondeclarative part: the violation. Its control 1985-GR-XIII_4_8_4-2
      *> performs a declarative paragraph there. WiseOwl compiles it and
      *> warns (COBOL2375, docs/CONFORMANCE.md D-DECLREF; kb/Work PB4995,
      *> PB5461).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DREFRWF.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RW-FILE ASSIGN TO "drefrw.dat"
               FILE STATUS IS RW-STATUS.
       DATA DIVISION.
       FILE SECTION.
       FD  RW-FILE
           REPORT IS RW-RPT.
       WORKING-STORAGE SECTION.
       01  RW-STATUS PIC XX VALUE SPACES.
       REPORT SECTION.
       RD  RW-RPT.
       01  RW-DTL TYPE IS DETAIL LINE PLUS 1.
           05  COLUMN 1 PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       DECLARATIVES.
       RW-ERR SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON RW-FILE.
       RW-ERR-1.
           DISPLAY "USE PROCEDURE STATUS " RW-STATUS.
<<         PERFORM P-1.
           GO TO RW-ERR-END.
       RW-ERR-NOTE.
           DISPLAY "DECLARATIVE PARAGRAPH PERFORMED".
       RW-ERR-SKIPPED.
           DISPLAY "GO TO DID NOT TRANSFER".
       RW-ERR-END.
           STOP RUN.
       END DECLARATIVES.
       MAIN-1 SECTION.
       P-1.
           DISPLAY "OPEN EXTEND OF A FILE NOT PRESENT".
           OPEN EXTEND RW-FILE.
           DISPLAY "RETURNED TO THE MAIN PROCEDURE".
           STOP RUN.
