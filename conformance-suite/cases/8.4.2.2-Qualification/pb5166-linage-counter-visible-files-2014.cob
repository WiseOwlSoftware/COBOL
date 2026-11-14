      *>--- conformance-case
      *> id: pb5166-linage-counter-visible-files-2014
      *> rules: [SR-8.4.2.2.3-8]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5166, §2014:8.4.1.1.2 SR8 (§2023:8.4.2.2.3 SR8): the register may be
      *> referenced "within the source element in which the reference to
      *> LINAGE-COUNTER occurs", so the container's GLOBAL LINAGE file
      *> (PFILE1, visible in the contained program) and the contained
      *> program's own PFILE2 make two, and the unqualified reference is
      *> refused from 2014. At 85 and 2002 the same text is legal (§2002:8.4.1.1.2 SR6)
      *> (pb5166-linage-counter-own-entries-1985).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5166C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE1 ASSIGN TO "PB5166C1.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  PFILE1 IS GLOBAL LINAGE IS 20 LINES.
       01  P1-REC PIC X(20).
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB5166D".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5166D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT PFILE2 ASSIGN TO "PB5166C2.DAT".
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
       END PROGRAM PB5166D.
       END PROGRAM PB5166C.
