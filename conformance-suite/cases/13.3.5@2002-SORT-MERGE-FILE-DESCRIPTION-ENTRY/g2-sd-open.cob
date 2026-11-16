      *>--- conformance-case
      *> id: g2-sd-open
      *> rules: [2002-SR-13.3.5.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.3.5.2 SR3 - "File-name-1 shall not be specified in an
      *> input-output statement." S is the file-name of an SD entry;
      *> the statement "OPEN INPUT S" (OPEN) is one of the
      *> input-output statements §2002:9.1.12 names, so the case is
      *> refused at 2002 (COBOL1692). Why it can fail: the statement is
      *> accepted against the sort file, or another code is reported.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SD02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT S ASSIGN TO "g2sd02.tmp".
       DATA DIVISION.
       FILE SECTION.
       SD  S.
       01  S-REC.
           05 S-KEY PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN INPUT S.
           DISPLAY "COMPILED".
           STOP RUN.
       END PROGRAM G2SD02.
