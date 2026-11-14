      *>--- conformance-case
      *> id: pb5166-linage-counter-own-entries-2002
      *> rules: [2002-SR-8.4.1.1.2-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.1.1.2 SR6: LINAGE-COUNTER "shall be qualified if more
      *> than one file description entry containing a LINAGE clause has
      *> been specified in the source element". The inner program
      *> specifies one (PFILE2); the GLOBAL LINAGE file of the container
      *> belongs to the containing source element (§2002:4, definition
      *> 4.172: a source element excludes its contained source units).
      *> So the unqualified reference is legal at 2002 and is PFILE2's
      *> register: after OPEN OUTPUT it holds 1, displayed "01". From
      *> 2014 the visible count applies and the program is refused
      *> (pb5166-linage-counter-visible-files-2014).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5166A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE1 ASSIGN TO "PB5166A1.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE1 IS GLOBAL LINAGE IS 20 LINES.
       01  P1-REC PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB5166B".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5166B.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE2 ASSIGN TO "PB5166A2.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE2 LINAGE IS 30 LINES.
       01  P2-REC PIC X(20).
       PROCEDURE DIVISION.
       IN-PARA.
           OPEN OUTPUT PFILE2.
           DISPLAY LINAGE-COUNTER.
           CLOSE PFILE2.
           EXIT PROGRAM.
       END PROGRAM PB5166B.
       END PROGRAM PB5166A.
