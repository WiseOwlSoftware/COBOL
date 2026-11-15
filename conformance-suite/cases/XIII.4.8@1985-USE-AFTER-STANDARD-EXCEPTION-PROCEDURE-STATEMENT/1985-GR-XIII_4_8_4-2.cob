      *>--- conformance-case
      *> id: 1985-GR-XIII_4_8_4-2
      *> rules: [1985-GR-XIII.4.8.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIII.4.8.4 GR2: "Within a declarative procedure, there
      *> must be no reference to any nondeclarative procedures." The
      *> legal form: the USE procedure references only declarative
      *> procedures (a PERFORM of RW-ERR-NOTE and a GO TO RW-ERR-END).
      *> OPEN EXTEND of a report file that is not present fails with
      *> I-O status 35 (§1985:VII.1.3.5), so the USE procedure runs
      *> (GR5). After a critical error the implementor determines what
      *> follows (GR6), so the procedure ends the run itself. WiseOwl
      *> warns (COBOL2375) on the violating form, a reference to a
      *> nondeclarative procedure (docs/CONFORMANCE.md D-DECLREF;
      *> kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DREFRW.
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
           PERFORM RW-ERR-NOTE.
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
