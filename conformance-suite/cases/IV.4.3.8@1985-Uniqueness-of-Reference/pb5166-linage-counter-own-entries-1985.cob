      *>--- conformance-case
      *> id: pb5166-linage-counter-own-entries-1985
      *> rules: [1985-SR-IV.4.3.8.1-L2.8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.1 SRL2.8: LINAGE-COUNTER is qualified only "if
      *> more than one file description entry containing a LINAGE clause
      *> has been specified in the source program". LCIN specifies one
      *> (PFILE2); the GLOBAL LINAGE file of LCOUT belongs to the
      *> containing program (§1985:X.1.3.8: each program's reference is
      *> to the object that program describes). So the unqualified
      *> reference is legal at 85 and is PFILE2's register: after OPEN
      *> OUTPUT it holds 1, displayed as the PIC 9(n) register "01".
      *> Fails if the resolver counts the container's GLOBAL file too
      *> (COBOL5997 at 85). From 2002 the visible count applies and the
      *> program is refused (the diagnostic file beside this case).
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
